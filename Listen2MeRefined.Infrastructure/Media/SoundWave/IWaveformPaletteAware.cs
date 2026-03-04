using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public interface IWaveformPaletteAware
{
    void UpdatePalette(SKColor lineColor, SKColor backgroundColor);
}
