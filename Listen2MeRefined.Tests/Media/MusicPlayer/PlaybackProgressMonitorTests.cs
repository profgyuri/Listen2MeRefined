using Listen2MeRefined.Infrastructure.Media.MusicPlayer;

namespace Listen2MeRefined.Tests.Media.MusicPlayer;

public sealed class PlaybackProgressMonitorTests
{
    [Fact]
    public void ShouldAdvance_OnlyAdvancesAtBoundary()
    {
        var monitor = new PlaybackProgressMonitor();
        monitor.Reset();

        Assert.False(monitor.ShouldAdvance(TimeSpan.FromSeconds(9.2), TimeSpan.FromSeconds(10), true));
        Assert.False(monitor.ShouldAdvance(TimeSpan.FromSeconds(9.2), TimeSpan.FromSeconds(10), true));
        Assert.True(monitor.ShouldAdvance(TimeSpan.FromSeconds(9.2), TimeSpan.FromSeconds(10), true));

        monitor.Reset();
        Assert.False(monitor.ShouldAdvance(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), true));
        Assert.False(monitor.ShouldAdvance(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), true));
        Assert.False(monitor.ShouldAdvance(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), true));
    }
}
