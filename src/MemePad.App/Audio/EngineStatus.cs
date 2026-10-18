namespace MemePad.App.Audio;

public enum EngineStatus
{
    /// <summary>Not started yet.</summary>
    Stopped,

    /// <summary>VB-Cable is missing; sounds still reach the monitor output.</summary>
    NoCable,

    Running,

    /// <summary>A device failed or changed; the engine is retrying.</summary>
    Reconnecting,
}
