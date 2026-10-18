using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemePad.App.Audio;
using MemePad.App.Localization;
using MemePad.Core.Audio;
using MemePad.Core.Input;
using MemePad.Core.Model;

namespace MemePad.App.ViewModels;

public sealed partial class PageItem(HotkeyPage model) : ObservableObject
{
    [ObservableProperty]
    private HotkeyPage _model = model;

    public string Name => Model.Name;

    partial void OnModelChanged(HotkeyPage value) => OnPropertyChanged(nameof(Name));
}

public sealed partial class MainViewModel : ObservableObject
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".mp3", ".wav", ".ogg", ".flac" };

    private readonly AppSettings _settings;
    private readonly IAppServices _services;
    private readonly List<SoundRowViewModel> _all = [];

    public MainViewModel(AppSettings settings, IAppServices services)
    {
        _settings = settings;
        _services = services;
        foreach (var p in settings.Pages) Pages.Add(new PageItem(p));
        _selectedPage = Pages[Math.Clamp(settings.ActivePage, 0, Pages.Count - 1)];
        foreach (var s in settings.Sounds) _all.Add(new SoundRowViewModel(s));
        _micVolume = settings.MicVolume;
        _soundsVolume = settings.SoundsVolume;
        UpdateHotkeyTexts();
        ApplyFilter();
        services.Playback.FormatChanged += () => _ = RefreshAsync();
    }

    public ObservableCollection<SoundRowViewModel> Rows { get; } = [];
    public ObservableCollection<PageItem> Pages { get; } = [];

    /// <summary>Raised when the live hotkey map changed (page switch, new binding, settings).</summary>
    public event Action? BindingsChanged;

    [ObservableProperty]
    private PageItem? _selectedPage;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private bool _showFavoritesOnly;

    [ObservableProperty]
    private string _speechText = "";

    [ObservableProperty]
    private float _micVolume;

    [ObservableProperty]
    private float _soundsVolume;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(IsCableMissing))]
    private EngineStatus _engineStatus = EngineStatus.Stopped;

    public string StatusText => EngineStatus switch
    {
        EngineStatus.Running => L.StatusRunning,
        EngineStatus.NoCable => L.StatusNoCable,
        EngineStatus.Reconnecting => L.StatusReconnecting,
        _ => L.StatusStopped,
    };

    public bool IsCableMissing => EngineStatus == EngineStatus.NoCable;
    public string StopHotkeyText => _settings.StopHotkey?.ToString() ?? "";
    public AppSettings Settings => _settings;

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnShowFavoritesOnlyChanged(bool value) => ApplyFilter();

    partial void OnSelectedPageChanged(PageItem? value)
    {
        if (value is null) return;
        _settings.ActivePage = Pages.IndexOf(value);
        UpdateHotkeyTexts();
        _services.Save(_settings);
        BindingsChanged?.Invoke();
    }

    partial void OnMicVolumeChanged(float value) => ApplyVolumes();
    partial void OnSoundsVolumeChanged(float value) => ApplyVolumes();

    private void ApplyVolumes()
    {
        _settings.MicVolume = MicVolume;
        _settings.SoundsVolume = SoundsVolume;
        _services.ApplyAudioSettings(_settings);
        _services.Save(_settings);
    }

    /// <summary>Decodes every library sound in the background and updates row state and duration.</summary>
    public async Task RefreshAsync()
    {
        var rows = _all.ToList();
        var playback = _services.Playback;
        var results = await Task.Run(() => rows.Select(r => playback.Decode(r.Sound.Path, r.Sound.Id)).ToList());
        for (var i = 0; i < rows.Count; i++) Apply(rows[i], results[i]);
    }

    private static void Apply(SoundRowViewModel row, DecodeResult result)
    {
        row.Decoded = result.Sound;
        row.Error = result.Error;
        row.State = result.Sound is not null ? SoundState.Ok
            : File.Exists(row.Sound.Path) ? SoundState.Unreadable
            : SoundState.Missing;
    }

    /// <summary>Hotkey map for the matcher: the active page's sounds plus global actions.</summary>
    public IReadOnlyDictionary<Hotkey, object> CurrentBindings()
    {
        var map = new Dictionary<Hotkey, object>();
        if (SelectedPage is not null)
            foreach (var (soundId, key) in SelectedPage.Model.Bindings) map[key] = new PlaySoundAction(soundId);

        void Global(Hotkey? key, GlobalAction action)
        {
            if (key is { } k) map[k] = action;
        }

        Global(_settings.StopHotkey, GlobalAction.Stop);
        Global(_settings.NextPageHotkey, GlobalAction.NextPage);
        Global(_settings.PrevPageHotkey, GlobalAction.PrevPage);
        Global(_settings.RecordHotkey, GlobalAction.Record);
        Global(_settings.EffectToggleHotkey, GlobalAction.ToggleEffect);
        return map;
    }

    /// <summary>Runs a hotkey action. Called on the UI thread.</summary>
    public void Execute(object action)
    {
        switch (action)
        {
            case PlaySoundAction p:
                if (_all.FirstOrDefault(r => r.Sound.Id == p.SoundId) is { } row) Play(row);
                break;
            case GlobalAction.Stop:
                StopAll();
                break;
            case GlobalAction.NextPage:
                StepPage(+1);
                break;
            case GlobalAction.PrevPage:
                StepPage(-1);
                break;
            case GlobalAction.Record:
                SaveClip();
                break;
            case GlobalAction.ToggleEffect:
                _settings.EffectEnabled = !_settings.EffectEnabled;
                _services.ApplyAudioSettings(_settings);
                _services.Save(_settings);
                break;
        }
    }

    private void StepPage(int delta)
    {
        if (Pages.Count == 0 || SelectedPage is null) return;
        var i = (Pages.IndexOf(SelectedPage) + delta + Pages.Count) % Pages.Count;
        SelectedPage = Pages[i];
    }

    /// <summary>Refreshes the playing indicator; called by the view on a short timer.</summary>
    public void UpdatePlaying()
    {
        var playing = _services.Playback.PlayingIds;
        foreach (var row in _all) row.IsPlaying = playing.Contains(row.Sound.Id);
    }

    [RelayCommand]
    private void Play(SoundRowViewModel? row)
    {
        if (row?.Decoded is not { } decoded || row.State != SoundState.Ok) return;
        var s = row.Sound;
        _services.Playback.Play(decoded, s.GainDb, s.TrimStart, s.TrimEnd, _settings.Mode);
    }

    [RelayCommand]
    private void StopAll() => _services.Playback.StopAll();

    public async Task AddFilesAsync(IEnumerable<string> paths)
    {
        var known = new HashSet<string>(_settings.Sounds.Select(s => s.Path), StringComparer.OrdinalIgnoreCase);
        var added = new List<SoundRowViewModel>();
        foreach (var path in paths)
        {
            if (!SupportedExtensions.Contains(Path.GetExtension(path)) || !known.Add(path)) continue;
            added.Add(AddSound(new Sound(Guid.NewGuid(), path, Path.GetFileNameWithoutExtension(path), false, 0, TimeSpan.Zero, null)));
        }
        if (added.Count == 0) return;

        _services.Save(_settings);
        var playback = _services.Playback;
        var results = await Task.Run(() => added.Select(r => playback.Decode(r.Sound.Path, r.Sound.Id)).ToList());
        for (var i = 0; i < added.Count; i++) Apply(added[i], results[i]);
    }

    private SoundRowViewModel AddSound(Sound sound)
    {
        _settings.Sounds.Add(sound);
        var row = new SoundRowViewModel(sound);
        _all.Add(row);
        ApplyFilter();
        return row;
    }

    private async void AddGeneratedSound(string path, string title)
    {
        var row = AddSound(new Sound(Guid.NewGuid(), path, title, false, 0, TimeSpan.Zero, null));
        _services.Save(_settings);
        var playback = _services.Playback;
        Apply(row, await Task.Run(() => playback.Decode(path, row.Sound.Id)));
    }

    [RelayCommand]
    private void Speak()
    {
        var result = _services.Speak(SpeechText, _settings);
        if (result.Sound is { } s) _services.Playback.PlayTransient(s);
    }

    [RelayCommand]
    private void SaveSpeech()
    {
        if (string.IsNullOrWhiteSpace(SpeechText)) return;
        if (_services.SaveSpeechFile(SpeechText, _settings) is { } path)
            AddGeneratedSound(path, string.Format(L.SpeechTitle, SpeechText.Trim()));
    }

    private void SaveClip()
    {
        if (_services.SaveClip() is { } path) AddGeneratedSound(path, string.Format(L.ClipTitle, DateTime.Now));
    }

    [RelayCommand]
    private void ToggleFavorite(SoundRowViewModel? row)
    {
        if (row is null) return;
        Replace(row, row.Sound with { Favorite = !row.Sound.Favorite });
    }

    [RelayCommand]
    private void Remove(SoundRowViewModel? row)
    {
        if (row is null) return;
        _settings.Sounds.RemoveAll(s => s.Id == row.Sound.Id);
        foreach (var p in _settings.Pages) p.Bindings.Remove(row.Sound.Id);
        _all.Remove(row);
        ApplyFilter();
        _services.Save(_settings);
        BindingsChanged?.Invoke();
    }

    [RelayCommand]
    private void EditSound(SoundRowViewModel? row)
    {
        if (row?.Decoded is not { } decoded) return;
        if (_services.Dialogs.EditSound(row.Sound, decoded, out var edited)) Replace(row, edited);
    }

    [RelayCommand]
    private void AssignHotkey(SoundRowViewModel? row)
    {
        if (row is null || SelectedPage is null) return;
        var page = SelectedPage.Model;
        var result = _services.Dialogs.CaptureHotkey(row.Title, key =>
        {
            var other = HotkeyMatcher.FindConflicts(page, key, row.Sound.Id).FirstOrDefault();
            return other == Guid.Empty ? null : _all.FirstOrDefault(r => r.Sound.Id == other)?.Title ?? "?";
        });
        if (result is null) return;

        if (result.Key is { } key)
        {
            foreach (var other in HotkeyMatcher.FindConflicts(page, key, row.Sound.Id)) page.Bindings.Remove(other);
            page.Bindings[row.Sound.Id] = key;
        }
        else
        {
            page.Bindings.Remove(row.Sound.Id);
        }

        UpdateHotkeyTexts();
        _services.Save(_settings);
        BindingsChanged?.Invoke();
    }

    [RelayCommand]
    private void AddPage()
    {
        if (_services.Dialogs.AskText(L.AddPage, "") is not { Length: > 0 } name) return;
        var page = AppSettings.NewPage(name.Trim());
        _settings.Pages.Add(page);
        var item = new PageItem(page);
        Pages.Add(item);
        SelectedPage = item;
    }

    [RelayCommand]
    private void RenamePage(PageItem? item)
    {
        item ??= SelectedPage;
        if (item is null || _services.Dialogs.AskText(L.RenamePage, item.Name) is not { Length: > 0 } name) return;
        var index = _settings.Pages.FindIndex(p => p.Id == item.Model.Id);
        var renamed = item.Model with { Name = name.Trim() };
        _settings.Pages[index] = renamed;
        item.Model = renamed;
        _services.Save(_settings);
    }

    [RelayCommand]
    private void DeletePage(PageItem? item)
    {
        item ??= SelectedPage;
        if (item is null || Pages.Count <= 1) return;
        var wasSelected = item == SelectedPage;
        _settings.Pages.RemoveAll(p => p.Id == item.Model.Id);
        Pages.Remove(item);
        if (wasSelected) SelectedPage = Pages[0];
        else _settings.ActivePage = Pages.IndexOf(SelectedPage!);
        _services.Save(_settings);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (!_services.Dialogs.EditSettings(_settings)) return;
        _services.ApplyAudioSettings(_settings);
        _services.Save(_settings);
        OnPropertyChanged(nameof(StopHotkeyText));
        BindingsChanged?.Invoke();
    }

    private void Replace(SoundRowViewModel row, Sound updated)
    {
        var index = _settings.Sounds.FindIndex(s => s.Id == updated.Id);
        if (index >= 0) _settings.Sounds[index] = updated;
        row.Sound = updated;
        ApplyFilter();
        _services.Save(_settings);
    }

    private void UpdateHotkeyTexts()
    {
        var bindings = SelectedPage?.Model.Bindings;
        foreach (var row in _all)
            row.HotkeyText = bindings is not null && bindings.TryGetValue(row.Sound.Id, out var key) ? key.ToString() : "";
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        var visible = _all.Where(r =>
            (!ShowFavoritesOnly || r.Favorite) &&
            (query.Length == 0 || r.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)));

        Rows.Clear();
        foreach (var r in visible) Rows.Add(r);
    }
}
