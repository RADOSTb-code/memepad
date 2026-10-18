using System.IO;
using System.Speech.Synthesis;
using MemePad.Core.Audio;
using NAudio.Wave;

namespace MemePad.App.Audio;

/// <summary>Turns a phrase into a sound using the Windows speech voices, decoded straight into the mixer format.</summary>
public sealed class SpeechSynth
{
    public IReadOnlyList<string> Voices()
    {
        using var synth = new SpeechSynthesizer();
        return synth.GetInstalledVoices().Where(v => v.Enabled).Select(v => v.VoiceInfo.Name).ToList();
    }

    /// <param name="rate">Speaking rate from -10 (slow) to 10 (fast); clamped.</param>
    public DecodeResult Synthesize(string text, string? voice, int rate, AudioFormat target)
    {
        if (string.IsNullOrWhiteSpace(text)) return new DecodeResult(null, "Text is empty");

        try
        {
            using var synth = new SpeechSynthesizer();
            if (voice is not null && synth.GetInstalledVoices().Any(v => v.Enabled && v.VoiceInfo.Name == voice))
                synth.SelectVoice(voice);
            synth.Rate = Math.Clamp(rate, -10, 10);

            using var wav = new MemoryStream();
            synth.SetOutputToWaveStream(wav);
            synth.Speak(text);
            synth.SetOutputToNull();

            wav.Position = 0;
            using var reader = new WaveFileReader(wav);
            return SoundDecoder.Decode(reader, target, Guid.NewGuid());
        }
        catch (Exception ex)
        {
            return new DecodeResult(null, ex.Message);
        }
    }
}
