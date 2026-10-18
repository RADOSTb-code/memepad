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

    /// <summary>Device and voice lists for the settings window.</summary>
    public Func<IReadOnlyList<DeviceOption>>? Microphones { get; set; }
    public Func<IReadOnlyList<DeviceOption>>? Outputs { get; set; }
    public Func<IReadOnlyList<string>>? Voices { get; set; }

    /// <summary>Called after settings are saved when the user chose "Restart as administrator".</summary>
    public Action? RestartElevated { get; set; }

    public HotkeyCapture? CaptureHotkey(string title, Func<Hotkey, string?> conflictCheck) =>
        CaptureHotkey(title, conflictCheck, VisibleOwner);

    private static HotkeyCapture? CaptureHotkey(string title, Func<Hotkey, string?> conflictCheck, Window? owner)
    {
        // Pause global hotkeys so pressing an already-bound combination does not also play its sound.
        using var _ = HotkeyGate.Suspend();
        var dialog = new HotkeyCaptureDialog(title, conflictCheck) { Owner = owner };
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

    public bool EditSettings(AppSettings settings)
    {
        SettingsWindow? window = null;
        var vm = new SettingsViewModel(
            settings,
            Microphones?.Invoke() ?? [],
            Outputs?.Invoke() ?? [],
            Voices?.Invoke() ?? [],
            title => CaptureHotkey(title, _ => null, window));
        window = new SettingsWindow(vm) { Owner = VisibleOwner };
        if (window.ShowDialog() != true) return false;

        vm.ApplyTo(settings);
        if (window.RestartRequested && RestartElevated is { } restart)
        {
            // Let the caller save first, then exit so the elevated copy can take over.
            Application.Current.Dispatcher.BeginInvoke(restart);
        }
        return true;
    }

    private Window? VisibleOwner => Owner is { IsVisible: true } ? Owner : null;
}
