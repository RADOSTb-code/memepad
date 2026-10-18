using System.Windows.Controls;
using System.Windows.Media;
using H.NotifyIcon;
using MemePad.App.Localization;

namespace MemePad.App.Tray;

/// <summary>Notification-area icon: click opens the window; menu offers Open, Stop and Exit.</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly TaskbarIcon _icon;

    public TrayIcon(Action open, Action stop, Action exit)
    {
        var menu = new ContextMenu();
        menu.Items.Add(Item(L.TrayOpen, open));
        menu.Items.Add(Item(L.Stop, stop));
        menu.Items.Add(Item(L.TrayExit, exit));

        _icon = new TaskbarIcon
        {
            ToolTipText = L.AppName,
            ContextMenu = menu,
            IconSource = new GeneratedIconSource
            {
                Text = "M",
                Foreground = new SolidColorBrush(Color.FromRgb(0x0D, 0x0E, 0x11)),
                Background = new SolidColorBrush(Color.FromRgb(0x7C, 0x9C, 0xFF)),
                FontWeight = System.Windows.FontWeights.Bold,
            },
        };
        _icon.TrayLeftMouseUp += (_, _) => open();
        _icon.ForceCreate();
    }

    private static MenuItem Item(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    public void Dispose() => _icon.Dispose();
}
