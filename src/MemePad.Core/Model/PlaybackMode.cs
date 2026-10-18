namespace MemePad.Core.Model;

public enum PlaybackMode
{
    /// <summary>A new sound stops whatever is playing.</summary>
    Interrupt,

    /// <summary>A new sound plays on top; retriggering the same sound restarts it.</summary>
    Overlap,
}

public enum VoiceEffectKind
{
    None,
    Higher,
    Lower,
    Robot,
}
