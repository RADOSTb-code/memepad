using MemePad.Core.Ptt;

namespace MemePad.Core.Tests;

public class PttControllerTests
{
    private sealed class FakeClock : IClock
    {
        public TimeSpan Now { get; set; }
    }

    private readonly FakeClock _clock = new();
    private int _presses;
    private int _releases;

    private PttController Make() =>
        new(_clock, () => _presses++, () => _releases++, TimeSpan.FromMilliseconds(200)) { Enabled = true };

    private void Advance(PttController p, int ms)
    {
        for (var t = 0; t < ms; t += 20)
        {
            _clock.Now += TimeSpan.FromMilliseconds(20);
            p.Tick();
        }
    }

    [Fact]
    public void Ptt_presses_once_releases_after_tail()
    {
        var p = Make();
        p.OnPlaybackStarted();
        Assert.Equal(1, _presses);
        Advance(p, 1000);
        Assert.Equal(0, _releases);

        p.OnPlaybackEnded();
        Advance(p, 180);
        Assert.Equal(0, _releases);
        Advance(p, 40);
        Assert.Equal(1, _releases);
        Advance(p, 500);
        Assert.Equal(1, _releases);
    }

    [Fact]
    public void Ptt_restart_within_tail_keeps_pressed()
    {
        var p = Make();
        p.OnPlaybackStarted();
        p.OnPlaybackEnded();
        Advance(p, 100);
        p.OnPlaybackStarted();
        Advance(p, 500);
        Assert.Equal(1, _presses);
        Assert.Equal(0, _releases);
    }

    [Fact]
    public void Ptt_disable_while_pressed_releases()
    {
        var p = Make();
        p.OnPlaybackStarted();
        p.Enabled = false;
        Assert.Equal(1, _releases);
        p.OnPlaybackEnded();
        Advance(p, 500);
        Assert.Equal(1, _releases);
    }

    [Fact]
    public void Ptt_disabled_never_presses()
    {
        var p = Make();
        p.Enabled = false;
        p.OnPlaybackStarted();
        p.OnPlaybackEnded();
        Advance(p, 500);
        Assert.Equal(0, _presses);
        Assert.Equal(0, _releases);
    }
}
