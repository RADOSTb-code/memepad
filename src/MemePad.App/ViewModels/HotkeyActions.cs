namespace MemePad.App.ViewModels;

/// <summary>Hotkey action: play one library sound.</summary>
public sealed record PlaySoundAction(Guid SoundId);

/// <summary>Hotkey actions that are not tied to a sound.</summary>
public enum GlobalAction
{
    Stop,
    NextPage,
    PrevPage,
    Record,
    ToggleEffect,
}
