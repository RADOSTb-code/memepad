using System.Runtime.InteropServices;
using MemePad.Core.Audio;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace MemePad.App.Audio;

/// <summary>
/// One live set of devices: microphone capture → engine render callback → VB-Cable output, plus a monitor output
/// fed with the sounds-only stream. Any device error is reported once through <see cref="PipelineRequest.OnFailed"/>.
/// </summary>
internal sealed class WasapiPipeline : IDisposable
{
    private const int OutputLatencyMs = 20;

    private readonly PipelineRequest _request;
    private readonly WasapiCapture? _capture;
    private readonly BufferedWaveProvider? _micBuffer;
    private readonly WasapiOut? _cableOut;
    private readonly WasapiOut? _monitorOut;
    private readonly BufferedWaveProvider? _monitorBuffer;
    private int _failed;
    private volatile bool _disposed;

    public WasapiPipeline(MMDeviceEnumerator enumerator, PipelineRequest request)
    {
        _request = request;
        var floatFormat = WaveFormat.CreateIeeeFloatWaveFormat(request.Format.SampleRate, request.Format.Channels);
        try
        {
            ISampleProvider? mic = null;
            var micDevice = ResolveMic(enumerator, request.MicId);
            if (micDevice is not null)
            {
                _capture = new WasapiCapture(micDevice, useEventSync: true, audioBufferMillisecondsLength: 20);
                _micBuffer = new BufferedWaveProvider(_capture.WaveFormat)
                {
                    BufferDuration = TimeSpan.FromMilliseconds(100),
                    DiscardOnBufferOverflow = true,
                    ReadFully = true,
                };
                _capture.DataAvailable += (_, e) => _micBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
                _capture.RecordingStopped += (_, e) => Fail(e.Exception);
                mic = SoundDecoder.Convert(_micBuffer.ToSampleProvider(), request.Format);
            }

            var monitorDevice = ResolveMonitor(enumerator, request.MonitorId, request.CableId);
            if (monitorDevice is not null)
            {
                _monitorBuffer = new BufferedWaveProvider(floatFormat)
                {
                    BufferDuration = TimeSpan.FromMilliseconds(200),
                    DiscardOnBufferOverflow = true,
                    ReadFully = true,
                };
                _monitorOut = new WasapiOut(monitorDevice, AudioClientShareMode.Shared, true, 30);
                _monitorOut.PlaybackStopped += (_, e) => Fail(e.Exception);
            }

            var mix = new MixProvider(floatFormat, mic, request.Render, request.CableId is not null ? _monitorBuffer : null);
            if (request.CableId is not null)
            {
                _cableOut = new WasapiOut(enumerator.GetDevice(request.CableId), AudioClientShareMode.Shared, true, OutputLatencyMs);
                _cableOut.PlaybackStopped += (_, e) => Fail(e.Exception);
                _cableOut.Init(mix);
                _monitorOut?.Init(_monitorBuffer);
            }
            else if (_monitorOut is not null)
            {
                // No cable: drive the mix from the monitor device so sounds can still be previewed.
                _monitorOut.Init(new MonitorOnlyProvider(mix));
            }

            _capture?.StartRecording();
            _cableOut?.Play();
            _monitorOut?.Play();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private static MMDevice? ResolveMic(MMDeviceEnumerator enumerator, string? micId)
    {
        if (micId is not null)
        {
            try { return enumerator.GetDevice(micId); }
            catch (COMException) { /* device gone: fall back to default */ }
        }

        if (enumerator.HasDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia))
        {
            var def = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            if (!DeviceCatalog.IsCableOutput(def.FriendlyName)) return def;
        }

        return enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .FirstOrDefault(d => !DeviceCatalog.IsCableOutput(d.FriendlyName));
    }

    private static MMDevice? ResolveMonitor(MMDeviceEnumerator enumerator, string? monitorId, string? cableId)
    {
        if (monitorId is not null && monitorId != cableId)
        {
            try { return enumerator.GetDevice(monitorId); }
            catch (COMException) { /* fall back to default */ }
        }

        if (!enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)) return null;
        var def = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        return def.ID == cableId ? null : def;
    }

    private void Fail(Exception? error)
    {
        if (error is null || _disposed) return;
        if (Interlocked.Exchange(ref _failed, 1) == 0) _request.OnFailed(error);
    }

    public void Dispose()
    {
        _disposed = true;
        try { _capture?.StopRecording(); } catch (COMException) { }
        _cableOut?.Dispose();
        _monitorOut?.Dispose();
        _capture?.Dispose();
    }

    /// <summary>Pulls mic audio, runs the engine callback, and forwards the monitor stream to its buffer.</summary>
    private sealed class MixProvider(WaveFormat format, ISampleProvider? mic, RenderCallback render, BufferedWaveProvider? monitorBuffer) : ISampleProvider
    {
        // Scratch buffers grow to the device's block size once and are then reused: no per-block allocation.
        private float[] _mic = [];
        private float[] _monitor = [];
        private float[] _output = [];
        private byte[] _monitorBytes = [];

        public WaveFormat WaveFormat { get; } = format;

        public int Read(float[] buffer, int offset, int count)
        {
            RenderBlock(buffer.AsSpan(offset, count));
            if (monitorBuffer is not null)
            {
                var bytes = MemoryMarshal.AsBytes(_monitor.AsSpan(0, count));
                bytes.CopyTo(_monitorBytes);
                monitorBuffer.AddSamples(_monitorBytes, 0, bytes.Length);
            }
            return count;
        }

        /// <summary>Renders once, returning the monitor stream instead of the cable stream.</summary>
        public int ReadMonitor(float[] buffer, int offset, int count)
        {
            Ensure(count);
            RenderBlock(_output.AsSpan(0, count));
            _monitor.AsSpan(0, count).CopyTo(buffer.AsSpan(offset, count));
            return count;
        }

        private void RenderBlock(Span<float> output)
        {
            var count = output.Length;
            Ensure(count);
            var micSpan = _mic.AsSpan(0, count);
            var got = mic?.Read(_mic, 0, count) ?? 0;
            micSpan[got..].Clear();
            render(output, micSpan, _monitor.AsSpan(0, count));
        }

        private void Ensure(int count)
        {
            if (_mic.Length >= count) return;
            _mic = new float[count];
            _monitor = new float[count];
            _output = new float[count];
            _monitorBytes = new byte[count * sizeof(float)];
        }
    }

    private sealed class MonitorOnlyProvider(MixProvider mix) : ISampleProvider
    {
        public WaveFormat WaveFormat => mix.WaveFormat;
        public int Read(float[] buffer, int offset, int count) => mix.ReadMonitor(buffer, offset, count);
    }
}
