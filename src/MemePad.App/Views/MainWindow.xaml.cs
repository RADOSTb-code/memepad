using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using MemePad.App.ViewModels;

namespace MemePad.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly DispatcherTimer _playingTimer;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        _playingTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, (_, _) => vm.UpdatePlaying(), Dispatcher);
        SourceInitialized += (_, _) => DarkTitleBar.Apply(this);
    }

    /// <summary>When false, closing the window only hides it to the tray.</summary>
    public bool AllowClose { get; set; }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _playingTimer.Stop();
        base.OnClosed(e);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files) await _vm.AddFilesAsync(files);
    }

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SoundList.SelectedItem is SoundRowViewModel row) _vm.PlayCommand.Execute(row);
    }

    private void OnListKeyDown(object sender, KeyEventArgs e)
    {
        if (SoundList.SelectedItem is not SoundRowViewModel row) return;
        if (e.Key == Key.Enter) _vm.PlayCommand.Execute(row);
        else if (e.Key == Key.Delete) _vm.RemoveCommand.Execute(row);
        else return;
        e.Handled = true;
    }

    private void OnSpeechKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        _vm.SpeakCommand.Execute(null);
        e.Handled = true;
    }
}

/// <summary>Asks Windows to draw the window caption dark so it matches the app.</summary>
internal static class DarkTitleBar
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int value, int size);

    public static void Apply(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var on = 1;
        _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int));
    }
}
