namespace MemePad.Core.Audio;

public static class Gain
{
    public static float DbToLinear(double db) => (float)Math.Pow(10, db / 20);
}
