using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MemePad.App.Localization;

namespace MemePad.App.Views;

/// <summary>Minimal one-line text prompt (page names).</summary>
internal sealed class TextPromptWindow : Window
{
    private readonly TextBox _box;

    private TextPromptWindow(string title, string initial)
    {
        Style = (Style)Application.Current.FindResource("DialogWindow");
        Title = title;
        Width = 380;
        SizeToContent = SizeToContent.Height;
        SourceInitialized += (_, _) => DarkTitleBar.Apply(this);

        _box = new TextBox { Text = initial, Style = (Style)FindResource("FieldBox"), Tag = L.PageName };
        _box.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) Accept();
        };

        var ok = new Button { Content = "OK", Style = (Style)FindResource("OutlineButton"), IsDefault = true, Margin = new Thickness(0, 16, 0, 0), HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) => Accept();

        Content = new StackPanel { Margin = new Thickness(20), Children = { _box, ok } };
        Loaded += (_, _) =>
        {
            _box.Focus();
            _box.SelectAll();
        };
    }

    private void Accept() => DialogResult = !string.IsNullOrWhiteSpace(_box.Text);

    public static string? Ask(Window? owner, string title, string initial)
    {
        var w = new TextPromptWindow(title, initial) { Owner = owner };
        return w.ShowDialog() == true ? w._box.Text.Trim() : null;
    }
}
