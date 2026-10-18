using System.Windows;
using System.Windows.Input;
using MemePad.App.Localization;
using MemePad.App.ViewModels;
using MemePad.Core.Input;

namespace MemePad.App.Views;

/// <summary>Waits for one key combination or mouse button 4/5. Esc cancels; Backspace removes the binding.</summary>
public partial class HotkeyCaptureDialog : Window
{
    private readonly Func<Hotkey, string?> _conflictCheck;
    private Hotkey? _pending;

    public HotkeyCaptureDialog(string soundTitle, Func<Hotkey, string?> conflictCheck)
    {
        InitializeComponent();
        Title = string.Format(L.HotkeyTitle, soundTitle);
        _conflictCheck = conflictCheck;
        SourceInitialized += (_, _) => DarkTitleBar.Apply(this);
    }

    public HotkeyCapture? Result { get; private set; }

    private bool AskingAboutConflict => ConflictPanel.Visibility == Visibility.Visible;

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (AskingAboutConflict) return;
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        switch (key)
        {
            case Key.Escape:
                DialogResult = false;
                return;
            case Key.Back when Keyboard.Modifiers == ModifierKeys.None:
                Finish(new HotkeyCapture(null));
                return;
            case Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin:
                return;   // wait for the main key
        }

        Propose(new Hotkey(CurrentModifiers(), KeyInterop.VirtualKeyFromKey(key)));
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (AskingAboutConflict) return;
        int vk;
        if (e.ChangedButton == MouseButton.XButton1) vk = 0x05;
        else if (e.ChangedButton == MouseButton.XButton2) vk = 0x06;
        else return;

        e.Handled = true;
        Propose(new Hotkey(CurrentModifiers(), vk));
    }

    private void Propose(Hotkey key)
    {
        KeyText.Text = key.ToString();
        var owner = _conflictCheck(key);
        if (owner is null)
        {
            Finish(new HotkeyCapture(key));
            return;
        }

        _pending = key;
        ConflictText.Text = string.Format(L.HotkeyConflict, owner);
        ConflictPanel.Visibility = Visibility.Visible;
        HelpText.Visibility = Visibility.Collapsed;
    }

    private void OnConflictAccept(object sender, RoutedEventArgs e)
    {
        if (_pending is { } key) Finish(new HotkeyCapture(key));
    }

    private void OnConflictCancel(object sender, RoutedEventArgs e)
    {
        _pending = null;
        KeyText.Text = "…";
        ConflictPanel.Visibility = Visibility.Collapsed;
        HelpText.Visibility = Visibility.Visible;
    }

    private void Finish(HotkeyCapture result)
    {
        Result = result;
        DialogResult = true;
    }

    private static Mods CurrentModifiers()
    {
        var held = Keyboard.Modifiers;
        var mods = Mods.None;
        if ((held & ModifierKeys.Control) != 0) mods |= Mods.Ctrl;
        if ((held & ModifierKeys.Alt) != 0) mods |= Mods.Alt;
        if ((held & ModifierKeys.Shift) != 0) mods |= Mods.Shift;
        if ((held & ModifierKeys.Windows) != 0) mods |= Mods.Win;
        return mods;
    }
}
