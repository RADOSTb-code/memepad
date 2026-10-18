using MemePad.Core.Audio;

namespace MemePad.App.Audio;

/// <summary>Fills one block: output = mic+sounds for the cable, monitor = sounds only. Same length for all three.</summary>
public delegate void RenderCallback(Span<float> output, ReadOnlySpan<float> mic, Span<float> monitor);

/// <summary>Everything a running pipeline needs from the engine.</summary>
public sealed record PipelineRequest(
    string? CableId,
    string? MicId,
    string? MonitorId,
    AudioFormat Format,
    RenderCallback Render,
    Action<Exception> OnFailed);

/// <summary>The seam between <see cref="AudioEngine"/> and the Windows audio stack, so reconnect logic is testable.</summary>
public interface IAudioEndpoints
{
    event Action? DevicesChanged;

    /// <summary>The VB-Cable render endpoint ("CABLE Input"), or null when the driver is missing.</summary>
    string? FindCableInputId();

    /// <summary>Shared-mode mix format of a render endpoint; null means the default output.</summary>
    AudioFormat GetMixFormat(string? renderId);

    /// <summary>Starts capture and playback; the returned handle stops them. May throw if a device cannot be opened.</summary>
    IDisposable OpenPipeline(PipelineRequest request);
}
