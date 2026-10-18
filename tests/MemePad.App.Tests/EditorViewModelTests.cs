using System.Security.Cryptography;
using MemePad.App.ViewModels;
using MemePad.Core.Audio;
using MemePad.Core.Model;

namespace MemePad.App.Tests;

public class EditorViewModelTests
{
    private static readonly AudioFormat Fmt = new(1000, 2);

    private static (Sound, DecodedSound) Make(string path = "x.wav", double seconds = 4)
    {
        var frames = (int)(Fmt.SampleRate * seconds);
        var samples = new float[frames * 2];
        for (var i = 0; i < frames; i++) samples[2 * i] = samples[2 * i + 1] = (float)Math.Sin(i / 10.0) * (i < frames / 2 ? 0.2f : 0.9f);
        var sound = new Sound(Guid.NewGuid(), path, "S", false, 0, TimeSpan.Zero, null);
        return (sound, new DecodedSound(sound.Id, Fmt, samples));
    }

    [Fact]
    public void Peaks_bucket_count_and_range()
    {
        var (s, d) = Make();
        var vm = new EditorViewModel(s, d, _ => { });

        var peaks = vm.Peaks(100);

        Assert.Equal(100, peaks.Length);
        Assert.All(peaks, p => Assert.InRange(p, 0f, 1f));
        Assert.True(peaks[90] > peaks[10]);   // the louder second half shows taller bars
    }

    [Fact]
    public void Defaults_cover_whole_sound()
    {
        var (s, d) = Make(seconds: 4);
        var vm = new EditorViewModel(s, d, _ => { });
        Assert.Equal(0, vm.TrimStartSeconds);
        Assert.Equal(4, vm.TrimEndSeconds, 3);
        Assert.Equal(4, vm.DurationSeconds, 3);
    }

    [Fact]
    public void Trim_end_clamped_after_start()
    {
        var (s, d) = Make(seconds: 4);
        var vm = new EditorViewModel(s, d, _ => { }) { TrimStartSeconds = 2 };

        vm.TrimEndSeconds = 1;
        Assert.True(vm.TrimEndSeconds > vm.TrimStartSeconds);

        vm.TrimEndSeconds = 10;
        Assert.Equal(4, vm.TrimEndSeconds, 3);

        vm.TrimStartSeconds = 5;
        Assert.True(vm.TrimStartSeconds < vm.TrimEndSeconds);
    }

    [Fact]
    public void Gain_clamped_to_minus24_plus12()
    {
        var (s, d) = Make();
        var vm = new EditorViewModel(s, d, _ => { }) { GainDb = 40 };
        Assert.Equal(12, vm.GainDb);
        vm.GainDb = -99;
        Assert.Equal(-24, vm.GainDb);
    }

    [Fact]
    public void Result_carries_edits_and_full_length_end_is_null()
    {
        var (s, d) = Make(seconds: 4);
        var vm = new EditorViewModel(s, d, _ => { }) { TrimStartSeconds = 0.5, GainDb = -3 };

        var edited = vm.Result();

        Assert.Equal(TimeSpan.FromSeconds(0.5), edited.TrimStart);
        Assert.Null(edited.TrimEnd);   // untouched end = play to the end of the file
        Assert.Equal(-3, edited.GainDb);

        vm.TrimEndSeconds = 3;
        Assert.Equal(TimeSpan.FromSeconds(3), vm.Result().TrimEnd);
    }

    [Fact]
    public void Preview_plays_current_edits()
    {
        var (s, d) = Make();
        Sound? previewed = null;
        var vm = new EditorViewModel(s, d, x => previewed = x) { GainDb = 6 };

        vm.PreviewCommand.Execute(null);

        Assert.Equal(6, previewed!.GainDb);
    }

    [Fact]
    public void Save_does_not_modify_source_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"memepad-src-{Guid.NewGuid():N}.wav");
        try
        {
            var (s, d) = Make(path);
            WavWriter.WriteFloat32(path, d.Format, d.Samples);
            var before = SHA256.HashData(File.ReadAllBytes(path));

            var vm = new EditorViewModel(s, d, _ => { }) { TrimStartSeconds = 1, TrimEndSeconds = 2, GainDb = 5 };
            _ = vm.Result();

            Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(path)));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
