using System.IO;
using System.Windows;
using System.Windows.Threading;
using MemePad.App.Audio;
using MemePad.App.Input;
using MemePad.App.Localization;
using MemePad.App.Services;
using MemePad.App.Tray;
using MemePad.App.ViewModels;
using MemePad.App.Views;
using MemePad.Core.Audio;
using MemePad.Core.Input;
using MemePad.Core.Model;
using MemePad.Core.Settings;

namespace MemePad.App;

public partial class App : Application
{
    /// <summary>%APPDATA%\MemePad, or MEMEPAD_DATA_DIR when set (portable use and testing).</summary>
    private static readonly string DataDir =
        Environment.GetEnvironmentVariable("MEMEPAD_DATA_DIR") is { Length: > 0 } custom
            ? custom
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MemePad");

    private Mutex? _singleInstance;
    private DeviceCatalog? _catalog;
    private AudioEngine? _engine;
    private LoopbackRecorder? _recorder;
    private AppServices? _services;
    private LowLevelInput? _input;
    private TrayIcon? _tray;
    private MainWindow? _window;
    private SettingsStore? _store;
    private AppSettings? _settings;
    private DispatcherTimer? _recorderRestart;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains("--selftest"))
        {
            Shutdown(SelfTest.Run());
            return;
        }

        _singleInstance = new Mutex(initiallyOwned: true, "MemePad.SingleInstance", out var isFirst);
        _store = new SettingsStore(DataDir);
        _settings = _store.Load();
        L.Use(_settings.Language);
        if (!isFirst)
        {
            MessageBox.Show(L.AlreadyRunning, L.AppName);
            Shutdown();
            return;
        }

        if (_settings.Pages.Count == 1 && _settings.Pages[0].Name == AppSettings.DefaultPageName)
            _settings.Pages[0] = _settings.Pages[0] with { Name = L.DefaultPage };

        var dialogs = new Dialogs();
        _catalog = new DeviceCatalog();
        _engine = new AudioEngine(_catalog);
        _recorder = new LoopbackRecorder(_settings.HistorySeconds);
        _services = new AppServices(DataDir, _store, _engine, _recorder, dialogs);

        var vm = new MainViewModel(_settings, _services);
        _engine.StatusChanged += s => Dispatcher.BeginInvoke(() => vm.EngineStatus = s);
        _services.Start(_settings);
        vm.EngineStatus = _engine.Status;

        // Re-open the loopback recorder a moment after device changes (default output may have moved).
        _recorderRestart = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _recorderRestart.Tick += (_, _) =>
        {
            _recorderRestart.Stop();
            _services.RestartRecorder();
        };
        _catalog.DevicesChanged += () => Dispatcher.BeginInvoke(() =>
        {
            _recorderRestart.Stop();
            _recorderRestart.Start();
        });

        StartHotkeys(vm);

        _window = new MainWindow(vm);
        dialogs.Owner = _window;
        _tray = new TrayIcon(ShowWindow, () => vm.StopAllCommand.Execute(null), ExitApp);
        _window.Show();
        _ = vm.RefreshAsync();
    }

    private void StartHotkeys(MainViewModel vm)
    {
        var matcher = new HotkeyMatcher();
        matcher.SetBindings(vm.CurrentBindings());
        vm.BindingsChanged += () => matcher.SetBindings(vm.CurrentBindings());

        _input = new LowLevelInput();
        _input.Input += ev =>
        {
            var actions = matcher.OnInput(ev);
            if (actions.Count == 0) return false;
            Dispatcher.BeginInvoke(() =>
            {
                foreach (var a in actions) vm.Execute(a);
            });
            return _settings!.SwallowHotkeys;
        };
        _input.Start();
    }

    private void ShowWindow()
    {
        if (_window is null) return;
        _window.Show();
        if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private void ExitApp()
    {
        if (_window is not null) _window.AllowClose = true;
        _window?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_settings is not null) _store?.Save(_settings);
        _input?.Dispose();
        _tray?.Dispose();
        _services?.Dispose();
        _recorder?.Dispose();
        _engine?.Dispose();
        _catalog?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}

/// <summary>
/// <c>MemePad.exe --selftest</c>: plays a 1 kHz tone for one second into VB-Cable and writes the outcome to
/// %TEMP%\memepad-selftest.log. Exit code 0 = played, 2 = no cable, 3 = engine did not start.
/// </summary>
internal static class SelfTest
{
    public static int Run()
    {
        var log = Path.Combine(Path.GetTempPath(), "memepad-selftest.log");
        using var catalog = new DeviceCatalog();
        using var engine = new AudioEngine(catalog);
        engine.Start(null, null);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (engine.Status is EngineStatus.Stopped or EngineStatus.Reconnecting && DateTime.UtcNow < deadline) Thread.Sleep(50);

        if (engine.Status != EngineStatus.Running)
        {
            File.WriteAllText(log, $"status={engine.Status}");
            return engine.Status == EngineStatus.NoCable ? 2 : 3;
        }

        var f = engine.MixFormat;
        var tone = new float[f.SampleRate * f.Channels];
        for (var i = 0; i < f.SampleRate; i++)
        {
            var v = 0.3f * (float)Math.Sin(2 * Math.PI * 1000 * i / f.SampleRate);
            for (var c = 0; c < f.Channels; c++) tone[i * f.Channels + c] = v;
        }
        engine.Mixer.Play(new DecodedSound(Guid.NewGuid(), f, tone), 0, TimeSpan.Zero, null, PlaybackMode.Interrupt);
        Thread.Sleep(1500);
        File.WriteAllText(log, $"status=Running format={f.SampleRate}/{f.Channels} played=1s");
        return 0;
    }
}
