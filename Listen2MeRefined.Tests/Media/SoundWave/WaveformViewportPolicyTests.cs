using Listen2MeRefined.Infrastructure.Media.SoundWave;

namespace Listen2MeRefined.Tests.Media.SoundWave;

public sealed class WaveformViewportPolicyTests
{
    [Theory]
    [InlineData(0, 100)]
    [InlineData(-1, 100)]
    [InlineData(double.NaN, 100)]
    [InlineData(double.PositiveInfinity, 100)]
    [InlineData(100, 0)]
    [InlineData(100, -1)]
    [InlineData(100, double.NaN)]
    [InlineData(100, double.PositiveInfinity)]
    public void TryNormalizeViewport_InvalidInput_ReturnsNull(double width, double height)
    {
        var sut = new WaveformViewportPolicy();

        var result = sut.TryNormalizeViewport(width, height);

        Assert.Null(result);
    }

    [Fact]
    public void TryNormalizeViewport_ValidInput_RoundsAndClamps()
    {
        var sut = new WaveformViewportPolicy();

        var result = sut.TryNormalizeViewport(63.6, 23.2);

        Assert.NotNull(result);
        Assert.Equal(64, result.Value.Width);
        Assert.Equal(24, result.Value.Height);
    }

    [Fact]
    public void HasMeaningfulChange_UsesResizeNoiseThreshold()
    {
        var sut = new WaveformViewportPolicy();

        Assert.False(sut.HasMeaningfulChange(400, 80, 402, 82));
        Assert.True(sut.HasMeaningfulChange(400, 80, 403, 82));
        Assert.True(sut.HasMeaningfulChange(400, 80, 402, 83));
    }
}
