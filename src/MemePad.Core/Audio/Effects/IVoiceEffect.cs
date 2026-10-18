namespace MemePad.Core.Audio.Effects;

/// <summary>An in-place, streaming effect on interleaved microphone audio. Keeps state between calls.</summary>
public interface IVoiceEffect
{
    void Process(Span<float> interleaved, AudioFormat format);
    void Reset();
}
