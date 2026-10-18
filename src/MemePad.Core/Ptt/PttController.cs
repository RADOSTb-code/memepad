namespace MemePad.Core.Ptt;

/// <summary>
/// Holds the push-to-talk key down while sounds play: presses on the first start, releases
/// <c>tail</c> after the last sound ends. A new start during the tail cancels the release.
/// </summary>
public sealed class PttController(IClock clock, Action press, Action release, TimeSpan tail)
{
    private readonly object _gate = new();
    private bool _pressed;
    private bool _enabled;
    private TimeSpan? _releaseAt;

    public bool Enabled
    {
        get { lock (_gate) return _enabled; }
        set
        {
            bool doRelease;
            lock (_gate)
            {
                _enabled = value;
                doRelease = !value && _pressed;
                if (doRelease)
                {
                    _pressed = false;
                    _releaseAt = null;
                }
            }
            if (doRelease) release();
        }
    }

    public void OnPlaybackStarted()
    {
        bool doPress;
        lock (_gate)
        {
            _releaseAt = null;
            doPress = _enabled && !_pressed;
            if (doPress) _pressed = true;
        }
        if (doPress) press();
    }

    public void OnPlaybackEnded()
    {
        lock (_gate)
        {
            if (_pressed) _releaseAt = clock.Now + tail;
        }
    }

    /// <summary>Called periodically (about every 20 ms) to release the key once the tail has elapsed.</summary>
    public void Tick()
    {
        bool doRelease;
        lock (_gate)
        {
            doRelease = _pressed && _releaseAt is { } at && clock.Now >= at;
            if (doRelease)
            {
                _pressed = false;
                _releaseAt = null;
            }
        }
        if (doRelease) release();
    }
}
