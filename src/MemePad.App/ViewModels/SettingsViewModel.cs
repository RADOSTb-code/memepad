using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemePad.App.Localization;
using MemePad.Core.Input;
using MemePad.Core.Model;

namespace MemePad.App.ViewModels;

/// <summary>An entry in a device list; a null <see cref="Id"/> means "system default".</summary>
public sealed record DeviceOption(string? Id, string Name)
{
    public override string ToString() => Name;
}

public enum HotkeySlot
{
    Stop,
    NextPage,
    PrevPage,
    Record,
    EffectToggle,
    PushToTalk,
}

public sealed record Choice<T>(T Value, string Label)
{
    public override string ToString() => Label;
}

/// <summary>
/// Edits a working copy of the settings; nothing changes until <see cref="ApplyTo"/>.
/// <paramref name="captureHotkey"/> opens the key capture dialog with the given title.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly Func<string, HotkeyCapture?> _captureHotkey;
    private readonly Dictionary<HotkeySlot, Hotkey?> _keys;
    private int _historySeconds;
    private int _ttsRate;

    public SettingsViewModel(
        AppSettings settings,
        IReadOnlyList<DeviceOption> microphones,
        IReadOnlyList<DeviceOption> outputs,
        IReadOnlyList<string> voices,
        Func<string, HotkeyCapture?> captureHotkey)
    {
        _captureHotkey = captureHotkey;
        Microphones = WithDefault(microphones, settings.MicDeviceId);
        Outputs = WithDefault(outputs, settings.MonitorDeviceId);
        _selectedMic = Microphones.First(d => d.Id == settings.MicDeviceId);
        _selectedMonitor = Outputs.First(d => d.Id == settings.MonitorDeviceId);

        Voices = [new Choice<string?>(null, L.SystemDefault), .. voices.Select(v => new Choice<string?>(v, v))];
        _ttsVoice = voices.Contains(settings.TtsVoice ?? "") ? settings.TtsVoice : null;

        _monitorEnabled = settings.MonitorEnabled;
        _monitorVolume = settings.MonitorVolume;
        _pttEnabled = settings.PttEnabled;
        _mode = settings.Mode;
        _swallowHotkeys = settings.SwallowHotkeys;
        _effect = settings.Effect;
        _effectEnabled = settings.EffectEnabled;
        _language = settings.Language;
        _historySeconds = Math.Clamp(settings.HistorySeconds, 10, 120);
        _ttsRate = Math.Clamp(settings.TtsRate, -5, 5);

        _keys = new()
        {
            [HotkeySlot.Stop] = settings.StopHotkey,
            [HotkeySlot.NextPage] = settings.NextPageHotkey,
            [HotkeySlot.PrevPage] = settings.PrevPageHotkey,
            [HotkeySlot.Record] = settings.RecordHotkey,
            [HotkeySlot.EffectToggle] = settings.EffectToggleHotkey,
            [HotkeySlot.PushToTalk] = settings.PttKey,
        };
    }

    public IReadOnlyList<DeviceOption> Microphones { get; }
    public IReadOnlyList<DeviceOption> Outputs { get; }
    public IReadOnlyList<Choice<string?>> Voices { get; }

    public IReadOnlyList<Choice<PlaybackMode>> Modes { get; } =
        [new(PlaybackMode.Interrupt, L.ModeInterrupt), new(PlaybackMode.Overlap, L.ModeOverlap)];

    public IReadOnlyList<Choice<VoiceEffectKind>> Effects { get; } =
    [
        new(VoiceEffectKind.None, L.EffectNone), new(VoiceEffectKind.Higher, L.EffectHigher),
        new(VoiceEffectKind.Lower, L.EffectLower), new(VoiceEffectKind.Robot, L.EffectRobot),
    ];

    public IReadOnlyList<Choice<string?>> Languages { get; } =
        [new(null, L.LangSystem), new("ru", "Русский"), new("en", "English")];

    [ObservableProperty] private DeviceOption? _selectedMic;
    [ObservableProperty] private DeviceOption? _selectedMonitor;
    [ObservableProperty] private bool _monitorEnabled;
    [ObservableProperty] private float _monitorVolume;
    [ObservableProperty] private bool _pttEnabled;
    [ObservableProperty] private PlaybackMode _mode;
    [ObservableProperty] private bool _swallowHotkeys;
    [ObservableProperty] private VoiceEffectKind _effect;
    [ObservableProperty] private bool _effectEnabled;
    [ObservableProperty] private string? _ttsVoice;
    [ObservableProperty] private string? _language;

    public int HistorySeconds
    {
        get => _historySeconds;
        set => SetProperty(ref _historySeconds, Math.Clamp(value, 10, 120));
    }

    public int TtsRate
    {
        get => _ttsRate;
        set => SetProperty(ref _ttsRate, Math.Clamp(value, -5, 5));
    }

    public string StopText => Text(HotkeySlot.Stop);
    public string NextPageText => Text(HotkeySlot.NextPage);
    public string PrevPageText => Text(HotkeySlot.PrevPage);
    public string RecordText => Text(HotkeySlot.Record);
    public string EffectToggleText => Text(HotkeySlot.EffectToggle);
    public string PttKeyText => Text(HotkeySlot.PushToTalk);

    private string Text(HotkeySlot slot) => _keys[slot]?.ToString() ?? L.NotSet;

    [RelayCommand]
    private void Capture(HotkeySlot slot)
    {
        var title = slot switch
        {
            HotkeySlot.Stop => L.KeyStop,
            HotkeySlot.NextPage => L.KeyNextPage,
            HotkeySlot.PrevPage => L.KeyPrevPage,
            HotkeySlot.Record => L.KeyRecord,
            HotkeySlot.EffectToggle => L.KeyEffect,
            _ => L.PttKey,
        };
        if (_captureHotkey(title) is not { } result) return;
        _keys[slot] = result.Key;
        OnPropertyChanged(slot switch
        {
            HotkeySlot.Stop => nameof(StopText),
            HotkeySlot.NextPage => nameof(NextPageText),
            HotkeySlot.PrevPage => nameof(PrevPageText),
            HotkeySlot.Record => nameof(RecordText),
            HotkeySlot.EffectToggle => nameof(EffectToggleText),
            _ => nameof(PttKeyText),
        });
    }

    public void ApplyTo(AppSettings s)
    {
        s.MicDeviceId = SelectedMic?.Id;
        s.MonitorDeviceId = SelectedMonitor?.Id;
        s.MonitorEnabled = MonitorEnabled;
        s.MonitorVolume = MonitorVolume;
        s.PttEnabled = PttEnabled;
        s.Mode = Mode;
        s.SwallowHotkeys = SwallowHotkeys;
        s.Effect = Effect;
        s.EffectEnabled = EffectEnabled;
        s.TtsVoice = TtsVoice;
        s.TtsRate = TtsRate;
        s.HistorySeconds = HistorySeconds;
        s.Language = Language;
        s.StopHotkey = _keys[HotkeySlot.Stop];
        s.NextPageHotkey = _keys[HotkeySlot.NextPage];
        s.PrevPageHotkey = _keys[HotkeySlot.PrevPage];
        s.RecordHotkey = _keys[HotkeySlot.Record];
        s.EffectToggleHotkey = _keys[HotkeySlot.EffectToggle];
        s.PttKey = _keys[HotkeySlot.PushToTalk];
    }

    /// <summary>"System default" first; a saved device that is not connected now stays selectable so its id is kept.</summary>
    private static List<DeviceOption> WithDefault(IReadOnlyList<DeviceOption> devices, string? savedId)
    {
        var list = new List<DeviceOption> { new(null, L.SystemDefault) };
        list.AddRange(devices);
        if (savedId is not null && devices.All(d => d.Id != savedId)) list.Add(new DeviceOption(savedId, string.Format(L.Unavailable, savedId)));
        return list;
    }
}
