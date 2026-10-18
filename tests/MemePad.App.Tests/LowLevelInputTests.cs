using System.Collections.Concurrent;
using MemePad.App.Input;
using MemePad.Core.Input;

namespace MemePad.App.Tests;

public class LowLevelInputTests
{
    private const int VK_F24 = 0x87;

    [Fact]
    public void Hook_ignores_own_injected_keys_but_sees_foreign_ones()
    {
        var seen = new ConcurrentQueue<InputEvent>();
        using var input = new LowLevelInput();
        input.Input += e =>
        {
            if (e.VirtualKey == VK_F24) seen.Enqueue(e);
            return false;
        };
        input.Start();

        KeySender.Down(VK_F24);              // signed as ours → must be skipped
        KeySender.Up(VK_F24);
        KeySender.SendUnsigned(VK_F24, down: true);   // looks like any other app → must be seen
        KeySender.SendUnsigned(VK_F24, down: false);

        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (seen.Count < 2 && DateTime.UtcNow < deadline) Thread.Sleep(10);
        Thread.Sleep(100);

        Assert.Equal([new InputEvent(VK_F24, true), new InputEvent(VK_F24, false)], seen.ToArray());
    }
}
