using System.Windows;
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

    public string? AskText(string title, string initial) => TextPromptWindow.Ask(VisibleOwner, title, initial);

    public HotkeyCapture? CaptureHotkey(string title, Func<Hotkey, string?> conflictCheck) => null;

    public bool EditSound(Sound sound, DecodedSound decoded, out Sound edited)
    {
        edited = sound;
        return false;
    }

    public bool EditSettings(AppSettings settings) => false;

    private Window? VisibleOwner => Owner is { IsVisible: true } ? Owner : null;
}
