using System.Windows;
using MemePad.App.Input;
using MemePad.App.ViewModels;
using MemePad.App.Views;
using MemePad.Core.Audio;
using MemePad.Core.Input;
using MemePad.Core.Model;

namespace MemePad.App.Services;

/// <summary>Opens the app's modal windows, owned by the main window.</summary>
public sealed class Dialogs : IDialogs
{
    public Window? Owner { get; set; }

    /// <summary>Used by the sound editor's Preview button.</summary>
    public IPlaybackService? Playback { get; set; }

    public string? AskText(string title, string initial) => TextPromptWindow.Ask(VisibleOwner, title, initial);

    public HotkeyCapture? CaptureHotkey(string title, Func<Hotkey, string?> conflictCheck)
    {
        // Pause global hotkeys so pressing an already-bound combination does not also play its sound.
        using var _ = HotkeyGate.Suspend();
        var dialog = new HotkeyCaptureDialog(title, conflictCheck) { Owner = VisibleOwner };
        return dialog.ShowDialog() == true ? dialog.Result : null;
    }

    public bool EditSound(Sound sound, DecodedSound decoded, out Sound edited)
    {
        var vm = new EditorViewModel(sound, decoded, preview =>
            Playback?.Play(decoded, preview.GainDb, preview.TrimStart, preview.TrimEnd, PlaybackMode.Interrupt));
        var window = new SoundEditorWindow(vm) { Owner = VisibleOwner };
        var saved = window.ShowDialog() == true;
        Playback?.StopAll();
        edited = saved ? vm.Result() : sound;
        return saved;
    }

    public bool EditSettings(AppSettings settings) => false;

    private Window? VisibleOwner => Owner is { IsVisible: true } ? Owner : null;
}
