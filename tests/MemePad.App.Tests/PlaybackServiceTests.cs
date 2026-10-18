using MemePad.App.Audio;
using MemePad.App.Services;
using MemePad.Core.Audio;

namespace MemePad.App.Tests;

public class PlaybackServiceTests
{
    private sealed class SwitchingEndpoints : IAudioEndpoints
    {
        public volatile int Rate = 48000;
        public event Action? DevicesChanged;
        public string? FindCableInputId() => "cable";
        public AudioFormat GetMixFormat(string? renderId) => new(Rate, 2);
        public IDisposable OpenPipeline(PipelineRequest request) => new Noop();
        public void Raise() => DevicesChanged?.Invoke();

        private sealed class Noop : IDisposable
        {
            public void Dispose() { }
        }
    }

    /// <summary>Stands in for the WPF dispatcher: records posts and remembers which thread raised them.</summary>
    private sealed class RecordingContext : SynchronizationContext
    {
        public int Posts;
        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref Posts);
            d(state);
        }
    }

    [Fact]
    public void Format_change_from_reconnect_thread_is_posted_to_the_ui_context()
    {
        var context = new RecordingContext();
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var endpoints = new SwitchingEndpoints();
            using var engine = new AudioEngine(endpoints, retryDelay: TimeSpan.FromMilliseconds(10));
            using var playback = new PlaybackService(engine);
            var raised = new ManualResetEventSlim();
            playback.FormatChanged += raised.Set;
            engine.Start(null, null);

            endpoints.Rate = 44100;
            endpoints.Raise();   // the engine reopens on a timer thread and finds a new format

            Assert.True(raised.Wait(TimeSpan.FromSeconds(5)));
            Assert.Equal(1, context.Posts);
            Assert.Equal(new AudioFormat(44100, 2), playback.Format);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
}
