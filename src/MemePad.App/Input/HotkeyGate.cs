namespace MemePad.App.Input;

/// <summary>Lets dialogs pause global hotkeys (e.g. while capturing a new key) without touching the hook.</summary>
public static class HotkeyGate
{
    private static int _suspended;

    public static bool IsSuspended => Volatile.Read(ref _suspended) > 0;

    public static IDisposable Suspend()
    {
        Interlocked.Increment(ref _suspended);
        return new Releaser();
    }

    private sealed class Releaser : IDisposable
    {
        private int _done;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _done, 1) == 0) Interlocked.Decrement(ref _suspended);
        }
    }
}
