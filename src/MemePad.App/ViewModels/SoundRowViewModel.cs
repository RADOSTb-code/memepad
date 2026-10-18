using CommunityToolkit.Mvvm.ComponentModel;
using MemePad.Core.Audio;
using MemePad.Core.Model;

namespace MemePad.App.ViewModels;

public enum SoundState
{
    Loading,
    Ok,
    Missing,
    Unreadable,
}

public sealed partial class SoundRowViewModel(Sound sound) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(Favorite), nameof(DurationText))]
    private Sound _sound = sound;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText), nameof(IsAvailable))]
    private SoundState _state = SoundState.Loading;

    [ObservableProperty]
    private string _hotkeyText = "";

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText))]
    private DecodedSound? _decoded;

    public string Title => Sound.Title;
    public bool Favorite => Sound.Favorite;
    public bool IsAvailable => State == SoundState.Ok;
    public string? Error { get; set; }

    /// <summary>Playable length after trimming, rounded up to whole seconds, as m:ss.</summary>
    public string DurationText
    {
        get
        {
            if (Decoded is null) return "";
            var end = Sound.TrimEnd is { } e && e < Decoded.Duration ? e : Decoded.Duration;
            var length = end - Sound.TrimStart;
            if (length < TimeSpan.Zero) length = TimeSpan.Zero;
            var seconds = (int)Math.Ceiling(length.TotalSeconds);
            return $"{seconds / 60}:{seconds % 60:00}";
        }
    }
}
