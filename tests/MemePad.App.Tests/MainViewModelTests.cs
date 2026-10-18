using MemePad.App.Audio;
using MemePad.App.ViewModels;
using MemePad.Core.Audio;
using MemePad.Core.Input;
using MemePad.Core.Model;

namespace MemePad.App.Tests;

public sealed class MainViewModelTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("memepad-vm-").FullName;
    private readonly FakeServices _services = new();

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private sealed class FakePlayback : IPlaybackService
    {
        public readonly List<Guid> Played = [];
        public AudioFormat Format => new(1000, 1);
        public IReadOnlyCollection<Guid> PlayingIds => Played;
        public event Action? FormatChanged { add { } remove { } }

        public DecodeResult Decode(string path, Guid id) =>
            File.Exists(path)
                ? new DecodeResult(new DecodedSound(id, Format, new float[2500]), null)   // 2.5 s
                : new DecodeResult(null, "File not found");

        public void Play(DecodedSound sound, double gainDb, TimeSpan trimStart, TimeSpan? trimEnd, PlaybackMode mode) => Played.Add(sound.Id);
        public void PlayTransient(DecodedSound sound) => Played.Add(sound.Id);
        public void StopAll() => Played.Clear();
    }

    private sealed class FakeServices : IAppServices
    {
        public readonly FakePlayback FakePlayback = new();
        public int Saves;
        public IPlaybackService Playback => FakePlayback;
        public IDialogs Dialogs { get; } = new NoDialogs();
        public void Save(AppSettings settings) => Saves++;
        public void ApplyAudioSettings(AppSettings settings) { }
        public DecodeResult Speak(string text, AppSettings settings) => new(null, "no tts in tests");
        public string? SaveSpeechFile(string text, AppSettings settings) => null;
        public string? SaveClip() => null;
        public int Retries;
        public readonly List<string> OpenedUrls = [];
        public void RetryAudio() => Retries++;
        public void OpenUrl(string url) => OpenedUrls.Add(url);
    }

    private sealed class NoDialogs : IDialogs
    {
        public HotkeyCapture? CaptureHotkey(string title, Func<Hotkey, string?> conflictCheck) => null;
        public bool EditSound(Sound sound, DecodedSound decoded, out Sound edited) { edited = sound; return false; }
        public bool EditSettings(AppSettings settings) => false;
        public string? AskText(string title, string initial) => null;
    }

    private string MakeFile(string name)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, [1, 2, 3]);
        return path;
    }

    private static Sound S(string path, string title, bool fav = false) =>
        new(Guid.NewGuid(), path, title, fav, 0, TimeSpan.Zero, null);

    [Fact]
    public async Task Search_filters_by_title_case_insensitive()
    {
        var settings = new AppSettings { Sounds = [S(MakeFile("a.mp3"), "Ба-дум-тсс"), S(MakeFile("b.mp3"), "Сверчки"), S(MakeFile("c.mp3"), "Бум")] };
        var vm = new MainViewModel(settings, _services);
        await vm.RefreshAsync();

        vm.SearchText = "БА";

        Assert.Equal(["Ба-дум-тсс"], vm.Rows.Select(r => r.Title));
    }

    [Fact]
    public async Task Favorites_only_filter()
    {
        var settings = new AppSettings { Sounds = [S(MakeFile("a.mp3"), "One", fav: true), S(MakeFile("b.mp3"), "Two")] };
        var vm = new MainViewModel(settings, _services);
        await vm.RefreshAsync();

        vm.ShowFavoritesOnly = true;

        Assert.Equal(["One"], vm.Rows.Select(r => r.Title));
    }

    [Fact]
    public async Task Missing_file_row_shows_Missing_state_and_play_is_noop()
    {
        var ok = S(MakeFile("ok.mp3"), "Ok");
        var gone = S(Path.Combine(_dir, "gone.mp3"), "Gone");
        var vm = new MainViewModel(new AppSettings { Sounds = [ok, gone] }, _services);
        await vm.RefreshAsync();

        var goneRow = vm.Rows.Single(r => r.Title == "Gone");
        Assert.Equal(SoundState.Missing, goneRow.State);
        vm.PlayCommand.Execute(goneRow);
        Assert.Empty(_services.FakePlayback.Played);

        var okRow = vm.Rows.Single(r => r.Title == "Ok");
        Assert.Equal(SoundState.Ok, okRow.State);
        Assert.Equal("0:03", okRow.DurationText);   // 2.5 s rounds up
        vm.PlayCommand.Execute(okRow);
        Assert.Equal([ok.Id], _services.FakePlayback.Played);
    }

    [Fact]
    public async Task Hotkeys_rebound_on_page_switch()
    {
        var a = S(MakeFile("a.mp3"), "A");
        var key = new Hotkey(Mods.Ctrl, 0x31);
        var settings = new AppSettings
        {
            Sounds = [a],
            Pages =
            [
                new HotkeyPage(Guid.NewGuid(), "One", new() { [a.Id] = key }),
                new HotkeyPage(Guid.NewGuid(), "Two", new()),
            ],
            StopHotkey = new Hotkey(Mods.None, 0x13),
        };
        var vm = new MainViewModel(settings, _services);
        await vm.RefreshAsync();
        var changes = 0;
        vm.BindingsChanged += () => changes++;

        Assert.Equal(new PlaySoundAction(a.Id), vm.CurrentBindings()[key]);
        Assert.Equal("Ctrl+1", vm.Rows.Single().HotkeyText);

        vm.SelectedPage = vm.Pages[1];

        Assert.Equal(1, changes);
        Assert.False(vm.CurrentBindings().ContainsKey(key));
        Assert.Equal(GlobalAction.Stop, vm.CurrentBindings()[new Hotkey(Mods.None, 0x13)]);
        Assert.Equal("", vm.Rows.Single().HotkeyText);
        Assert.Equal(1, settings.ActivePage);
    }

    [Fact]
    public async Task Hotkey_action_plays_sound_and_next_page_cycles()
    {
        var a = S(MakeFile("a.mp3"), "A");
        var settings = new AppSettings
        {
            Sounds = [a],
            Pages = [new HotkeyPage(Guid.NewGuid(), "One", new()), new HotkeyPage(Guid.NewGuid(), "Two", new())],
        };
        var vm = new MainViewModel(settings, _services);
        await vm.RefreshAsync();

        vm.Execute(new PlaySoundAction(a.Id));
        Assert.Equal([a.Id], _services.FakePlayback.Played);

        vm.Execute(GlobalAction.NextPage);
        Assert.Equal("Two", vm.SelectedPage!.Name);
        vm.Execute(GlobalAction.NextPage);
        Assert.Equal("One", vm.SelectedPage!.Name);
        vm.Execute(GlobalAction.PrevPage);
        Assert.Equal("Two", vm.SelectedPage!.Name);
    }

    [Fact]
    public void Discord_tip_shows_once_after_cable_appears()
    {
        var settings = new AppSettings();
        var vm = new MainViewModel(settings, _services);

        vm.EngineStatus = EngineStatus.NoCable;
        Assert.True(vm.IsCableMissing);
        Assert.False(vm.ShowDiscordTip);

        vm.EngineStatus = EngineStatus.Running;
        Assert.True(vm.ShowDiscordTip);

        vm.DismissTipCommand.Execute(null);
        Assert.False(vm.ShowDiscordTip);
        Assert.True(settings.CableTipShown);

        vm.EngineStatus = EngineStatus.Reconnecting;
        vm.EngineStatus = EngineStatus.Running;
        Assert.False(vm.ShowDiscordTip);
    }

    [Fact]
    public void Cable_banner_commands_retry_and_open_download_page()
    {
        var vm = new MainViewModel(new AppSettings(), _services);

        vm.RetryCableCommand.Execute(null);
        vm.DownloadCableCommand.Execute(null);

        Assert.Equal(1, _services.Retries);
        Assert.Equal(["https://vb-audio.com/Cable/"], _services.OpenedUrls);
    }

    [Fact]
    public async Task AddFiles_adds_supported_skips_duplicates_and_saves()
    {
        var vm = new MainViewModel(new AppSettings(), _services);
        await vm.RefreshAsync();
        var mp3 = MakeFile("boom.mp3");

        await vm.AddFilesAsync([mp3, MakeFile("notes.txt"), mp3]);

        var row = Assert.Single(vm.Rows);
        Assert.Equal("boom", row.Title);
        Assert.True(_services.Saves > 0);
    }
}
