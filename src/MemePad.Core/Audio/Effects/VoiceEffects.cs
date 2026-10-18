using MemePad.Core.Model;

namespace MemePad.Core.Audio.Effects;

public static class VoiceEffects
{
    private const double Semitones = 5;

    public static IVoiceEffect? Create(VoiceEffectKind kind) => kind switch
    {
        VoiceEffectKind.Higher => new PitchShiftEffect(Math.Pow(2, Semitones / 12)),
        VoiceEffectKind.Lower => new PitchShiftEffect(Math.Pow(2, -Semitones / 12)),
        VoiceEffectKind.Robot => new RobotEffect(),
        _ => null,
    };
}
