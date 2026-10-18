using MemePad.App.Audio;
using MemePad.Core.Audio;
using MemePad.Core.Input;
using MemePad.Core.Model;

namespace MemePad.App.ViewModels;

/// <summary>What the main view model needs from the audio engine.</summary>
public interface IPlaybackService
{
    AudioFormat Format { get; }

    /// <summary>Raised when <see cref="Format"/> changed; previously decoded sounds are stale.</summary>
    event Action? FormatChanged;

    IReadOnlyCollection<Guid> PlayingIds { get; }
    DecodeResult Decode(string path, Guid id);
    void Play(DecodedSound sound, double gainDb, TimeSpan trimStart, TimeSpan? trimEnd, PlaybackMode mode);

    /// <summary>Plays a one-off sound (text-to-speech) that is not part of the library.</summary>
    void PlayTransient(DecodedSound sound);

    void StopAll();
}

/// <summary>Result of the hotkey capture dialog: a new key, or null <see cref="Key"/> to remove the binding.</summary>
public sealed record HotkeyCapture(Hotkey? Key);

public interface IDialogs
{
    /// <summary>Waits for a key combination. <paramref name="conflictCheck"/> returns the title already using a key, or null.</summary>
    HotkeyCapture? CaptureHotkey(string title, Func<Hotkey, string?> conflictCheck);

    bool EditSound(Sound sound, DecodedSound decoded, out Sound edited);
    bool EditSettings(AppSettings settings);
    string? AskText(string title, string initial);
}

/// <summary>Side effects of the main window, behind one seam so the view model is testable.</summary>
public interface IAppServices
{
    IPlaybackService Playback { get; }
    IDialogs Dialogs { get; }
    void Save(AppSettings settings);

    /// <summary>Pushes volumes, devices, effect, PTT and recorder settings to the running services.</summary>
    void ApplyAudioSettings(AppSettings settings);

    DecodeResult Speak(string text, AppSettings settings);

    /// <summary>Synthesizes <paramref name="text"/> into a WAV file for the library; returns its path or null.</summary>
    string? SaveSpeechFile(string text, AppSettings settings);

    /// <summary>Saves the last N seconds of system output as a WAV file; returns its path or null.</summary>
    string? SaveClip();

    /// <summary>Reopens the audio devices now (the "Check again" button after installing the cable).</summary>
    void RetryAudio();

    /// <summary>Opens a link in the default browser; only called from a user click.</summary>
    void OpenUrl(string url);
}
