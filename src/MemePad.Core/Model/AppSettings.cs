using MemePad.Core.Input;

namespace MemePad.Core.Model;

public sealed class AppSettings
{
    public const string DefaultPageName = "General";

    public int SchemaVersion { get; set; } = 1;
    public List<Sound> Sounds { get; set; } = [];
    public List<HotkeyPage> Pages { get; set; } = [NewPage(DefaultPageName)];
    public int ActivePage { get; set; }

    public Hotkey? StopHotkey { get; set; }
    public Hotkey? NextPageHotkey { get; set; }
    public Hotkey? PrevPageHotkey { get; set; }
    public Hotkey? RecordHotkey { get; set; }
    public Hotkey? EffectToggleHotkey { get; set; }

    public PlaybackMode Mode { get; set; } = PlaybackMode.Interrupt;
    public bool SwallowHotkeys { get; set; }

    public float MicVolume { get; set; } = 1f;
    public float SoundsVolume { get; set; } = 0.8f;
    public float MonitorVolume { get; set; } = 0.6f;
    public bool MonitorEnabled { get; set; } = true;
    public string? MicDeviceId { get; set; }
    public string? MonitorDeviceId { get; set; }

    public bool PttEnabled { get; set; }
    public Hotkey? PttKey { get; set; }

    public int HistorySeconds { get; set; } = 30;

    public VoiceEffectKind Effect { get; set; } = VoiceEffectKind.None;
    public bool EffectEnabled { get; set; }

    public string? TtsVoice { get; set; }
    public int TtsRate { get; set; }

    /// <summary>UI language code ("ru", "en"); null follows the system.</summary>
    public string? Language { get; set; }

    public static HotkeyPage NewPage(string name) => new(Guid.NewGuid(), name, []);
}
