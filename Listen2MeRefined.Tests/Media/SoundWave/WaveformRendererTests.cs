using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using Moq;
using SkiaSharp;

namespace Listen2MeRefined.Tests.Media.SoundWave;

public sealed class WaveformRendererTests
{
    [Fact]
    public void SetSize_DelegatesToWaveformDrawer()
    {
        var drawer = new Mock<IWaveFormDrawer<SKBitmap>>();
        var sut = new WaveformRenderer(drawer.Object);

        sut.SetSize(800, 120);

        drawer.Verify(x => x.SetSize(800, 120), Times.Once);
    }

    [Fact]
    public async Task DrawPlaceholderAsync_ConcurrentCalls_AreSerialized()
    {
        var drawer = new Mock<IWaveFormDrawer<SKBitmap>>();
        var activeCalls = 0;
        var maxConcurrentCalls = 0;

        drawer.Setup(x => x.LineAsync()).Returns(async () =>
        {
            var current = Interlocked.Increment(ref activeCalls);
            if (current > maxConcurrentCalls)
            {
                maxConcurrentCalls = current;
            }

            await Task.Delay(25);
            Interlocked.Decrement(ref activeCalls);
            return new SKBitmap(1, 1);
        });

        var sut = new WaveformRenderer(drawer.Object);

        var first = sut.DrawPlaceholderAsync();
        var second = sut.DrawPlaceholderAsync();
        await Task.WhenAll(first, second);

        Assert.Equal(1, maxConcurrentCalls);
        drawer.Verify(x => x.LineAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task DrawTrackAsync_ValidPath_DrawsTrackWaveform()
    {
        var drawer = new Mock<IWaveFormDrawer<SKBitmap>>();
        drawer.Setup(x => x.WaveFormAsync("track.mp3")).ReturnsAsync(new SKBitmap(2, 2));
        var sut = new WaveformRenderer(drawer.Object);

        var bitmap = await sut.DrawTrackAsync("track.mp3");

        Assert.Equal(2, bitmap.Width);
        Assert.Equal(2, bitmap.Height);
        drawer.Verify(x => x.WaveFormAsync("track.mp3"), Times.Once);
    }
}
