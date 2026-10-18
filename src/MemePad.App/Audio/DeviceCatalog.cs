using MemePad.Core.Audio;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace MemePad.App.Audio;

/// <summary>Windows audio endpoints via WASAPI: lists devices, finds VB-Cable, and reports device changes.</summary>
public sealed class DeviceCatalog : IAudioEndpoints, IMMNotificationClient, IDisposable
{
    internal const string CableInputName = "CABLE Input";
    internal const string CableOutputName = "CABLE Output";

    private readonly MMDeviceEnumerator _enumerator = new();

    public DeviceCatalog() => _enumerator.RegisterEndpointNotificationCallback(this);

    public event Action? DevicesChanged;

    public string? FindCableInputId() =>
        Active(DataFlow.Render).FirstOrDefault(d => d.Name.Contains(CableInputName, StringComparison.OrdinalIgnoreCase)).Id;

    public IReadOnlyList<(string Id, string Name)> Microphones() =>
        Active(DataFlow.Capture).Where(d => !IsCableOutput(d.Name)).ToList();

    public IReadOnlyList<(string Id, string Name)> Outputs() =>
        Active(DataFlow.Render).Where(d => !d.Name.Contains(CableInputName, StringComparison.OrdinalIgnoreCase)).ToList();

    public AudioFormat GetMixFormat(string? renderId)
    {
        using var device = renderId is null ? _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia) : _enumerator.GetDevice(renderId);
        var mix = device.AudioClient.MixFormat;
        return new AudioFormat(mix.SampleRate, mix.Channels);
    }

    public IDisposable OpenPipeline(PipelineRequest request) => new WasapiPipeline(_enumerator, request);

    internal static bool IsCableOutput(string name) => name.Contains(CableOutputName, StringComparison.OrdinalIgnoreCase);

    private List<(string Id, string Name)> Active(DataFlow flow)
    {
        var list = new List<(string, string)>();
        foreach (var d in _enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
        {
            list.Add((d.ID, d.FriendlyName));
            d.Dispose();
        }
        return list;
    }

    public void Dispose()
    {
        _enumerator.UnregisterEndpointNotificationCallback(this);
        _enumerator.Dispose();
    }

    // COM notifications arrive on a system thread; the engine only schedules a retry from them.
    void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState) => DevicesChanged?.Invoke();
    void IMMNotificationClient.OnDeviceAdded(string pwstrDeviceId) => DevicesChanged?.Invoke();
    void IMMNotificationClient.OnDeviceRemoved(string deviceId) => DevicesChanged?.Invoke();
    void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (role == Role.Multimedia) DevicesChanged?.Invoke();
    }
    void IMMNotificationClient.OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
}
