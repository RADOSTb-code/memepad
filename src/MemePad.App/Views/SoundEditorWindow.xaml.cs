using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using MemePad.App.ViewModels;

namespace MemePad.App.Views;

public partial class SoundEditorWindow : Window
{
    private const int Buckets = 240;

    private readonly EditorViewModel _vm;
    private readonly float[] _peaks;

    public SoundEditorWindow(EditorViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        _peaks = vm.Peaks(Buckets);
        DataContext = vm;
        vm.PropertyChanged += OnVmChanged;
        SourceInitialized += (_, _) => DarkTitleBar.Apply(this);
    }

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorViewModel.TrimStartSeconds) or nameof(EditorViewModel.TrimEndSeconds)) UpdateCuts();
    }

    private void OnWaveSizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawWave(e.NewSize.Width, e.NewSize.Height);
        UpdateCuts();
    }

    /// <summary>Mirrored peak envelope: top edge left→right, then bottom edge right→left.</summary>
    private void DrawWave(double width, double height)
    {
        var points = new PointCollection(Buckets * 2);
        var mid = height / 2;
        var step = width / (Buckets - 1);
        for (var i = 0; i < Buckets; i++) points.Add(new Point(i * step, mid - Math.Max(0.5, _peaks[i] * (mid - 6))));
        for (var i = Buckets - 1; i >= 0; i--) points.Add(new Point(i * step, mid + Math.Max(0.5, _peaks[i] * (mid - 6))));
        Wave.Points = points;
    }

    private void UpdateCuts()
    {
        var width = WaveHost.ActualWidth;
        if (width <= 0 || _vm.DurationSeconds <= 0) return;
        CutLeft.Width = width * _vm.TrimStartSeconds / _vm.DurationSeconds;
        CutRight.Width = width * (1 - _vm.TrimEndSeconds / _vm.DurationSeconds);
    }

    private void OnSave(object sender, RoutedEventArgs e) => DialogResult = true;

    protected override void OnClosed(EventArgs e)
    {
        _vm.PropertyChanged -= OnVmChanged;
        base.OnClosed(e);
    }
}
