using MemePad.App.Audio;
using MemePad.Core.Audio;

namespace MemePad.App.Tests;

public class AudioEngineTests
{
    private sealed class FakeEndpoints : IAudioEndpoints
    {
        public int OpenAttempts;
        public int FailFirst = 1;
        public string? Cable = "cable-1";
        public PipelineRequest? LastRequest;

        public event Action? DevicesChanged;

        public string? FindCableInputId() => Cable;
        public AudioFormat GetMixFormat(string? renderId) => new(48000, 2);

        public IDisposable OpenPipeline(PipelineRequest request)
        {
            OpenAttempts++;
            if (OpenAttempts <= FailFirst) throw new InvalidOperationException("device busy");
            LastRequest = request;
            return new Pipeline();
        }

        public void RaiseDevicesChanged() => DevicesChanged?.Invoke();

        private sealed class Pipeline : IDisposable
        {
            public void Dispose() { }
        }
    }

    private static void WaitFor(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("condition not reached");
            Thread.Sleep(5);
        }
    }

    [Fact]
    public void Engine_reconnects_after_open_failure()
    {
        var fake = new FakeEndpoints { FailFirst = 2 };
        using var engine = new AudioEngine(fake, retryDelay: TimeSpan.FromMilliseconds(10));
        var statuses = new List<EngineStatus>();
        engine.StatusChanged += s => { lock (statuses) statuses.Add(s); };

        engine.Start(micId: null, monitorId: null);

        WaitFor(() => engine.Status == EngineStatus.Running);
        Assert.Equal(3, fake.OpenAttempts);
        lock (statuses) Assert.Contains(EngineStatus.Reconnecting, statuses);
    }

    [Fact]
    public void Engine_reconnects_after_devices_changed()
    {
        var fake = new FakeEndpoints { FailFirst = 0 };
        using var engine = new AudioEngine(fake, retryDelay: TimeSpan.FromMilliseconds(10));
        engine.Start(null, null);
        WaitFor(() => engine.Status == EngineStatus.Running);

        fake.RaiseDevicesChanged();

        WaitFor(() => fake.OpenAttempts == 2 && engine.Status == EngineStatus.Running);
    }

    [Fact]
    public void Engine_reports_no_cable_and_recovers_when_it_appears()
    {
        var fake = new FakeEndpoints { FailFirst = 0, Cable = null };
        using var engine = new AudioEngine(fake, retryDelay: TimeSpan.FromMilliseconds(10));
        engine.Start(null, null);
        WaitFor(() => engine.Status == EngineStatus.NoCable);

        fake.Cable = "cable-2";
        fake.RaiseDevicesChanged();

        WaitFor(() => engine.Status == EngineStatus.Running);
        Assert.Equal("cable-2", fake.LastRequest!.CableId);
    }

    [Fact]
    public void Pipeline_failure_triggers_reconnect()
    {
        var fake = new FakeEndpoints { FailFirst = 0 };
        using var engine = new AudioEngine(fake, retryDelay: TimeSpan.FromMilliseconds(10));
        engine.Start(null, null);
        WaitFor(() => engine.Status == EngineStatus.Running);

        fake.LastRequest!.OnFailed(new IOException("unplugged"));

        WaitFor(() => fake.OpenAttempts == 2 && engine.Status == EngineStatus.Running);
    }

    [Fact]
    public void Render_callback_mixes_mic_and_applies_monitor_volume()
    {
        var fake = new FakeEndpoints { FailFirst = 0 };
        using var engine = new AudioEngine(fake, retryDelay: TimeSpan.FromMilliseconds(10));
        engine.Start(null, null);
        WaitFor(() => engine.Status == EngineStatus.Running);
        engine.MonitorVolume = 0.5f;
        engine.Mixer.SoundsVolume = 1f;
        engine.Mixer.MicVolume = 1f;
        var tone = new float[960];
        Array.Fill(tone, 0.4f);
        engine.Mixer.Play(new DecodedSound(Guid.NewGuid(), engine.MixFormat, tone), 0, TimeSpan.Zero, null, Core.Model.PlaybackMode.Interrupt);

        var output = new float[96];
        var mic = new float[96];
        var monitor = new float[96];
        Array.Fill(mic, 0.1f);
        fake.LastRequest!.Render(output, mic, monitor);

        Assert.Equal(0.5f, output[0], 4);
        Assert.Equal(0.2f, monitor[0], 4);
    }
}
