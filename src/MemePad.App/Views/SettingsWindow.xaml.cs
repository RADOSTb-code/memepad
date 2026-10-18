using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using MemePad.App.ViewModels;

namespace MemePad.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        SourceInitialized += (_, _) => DarkTitleBar.Apply(this);
    }

    /// <summary>Set when the user chose to restart elevated; the caller saves settings and exits.</summary>
    public bool RestartRequested { get; private set; }

    private void OnSave(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnRunAsAdmin(object sender, RoutedEventArgs e)
    {
        if (Environment.ProcessPath is not { } exe) return;
        try
        {
            Process.Start(new ProcessStartInfo(exe, "--after-restart") { UseShellExecute = true, Verb = "runas" });
        }
        catch (Win32Exception)
        {
            return;   // the user declined the UAC prompt
        }
        RestartRequested = true;
        DialogResult = true;
    }
}
