using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using Moq;
using NAudio.Wave;
using Serilog;

namespace Listen2MeRefined.Tests.Media;

public class NAudioMusicPlayerOrchestrationTests
{
    [Fact]
    public async Task DeviceChangeWhilePlaying_ReconfiguresAndResumesWithTimestamp()
    {
        var track = new AudioModel { Path = "playing-track.mp3" };
        using var stream = CreateWaveStream();

        var logger = Mock.Of<ILogger>();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Publish(It.IsAny<CurrentSongNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var queue = new Mock<IPlaybackQueueService>();
        queue.Setup(x => x.GetCurrentTrack()).Returns(track);

        var loader = new Mock<ITrackLoader>();
        loader.Setup(x => x.Load(track)).Returns(TrackLoadResult.Success(stream));

        var output = new Mock<IPlaybackOutput>();
        output.SetupProperty(x => x.Volume, 1f);
        output
            .Setup(x => x.Reinitialize(It.IsAny<WaveStream>(), It.IsAny<int>()))
            .Returns(new PlaybackOutputReconfigureResult(true, false));

        var timedTask = new TimedTask();

        var player = new NAudioMusicPlayer(
            logger,
            mediator.Object,
            timedTask,
            queue.Object,
            loader.Object,
            output.Object,
            new PlaybackProgressMonitor());

        await player.PlayPauseAsync();
        player.CurrentTime = 1200;

        await player.Handle(new AudioOutputDeviceChangedNotification(new AudioOutputDevice(2, "Headphones")), CancellationToken.None);

        Assert.InRange(player.CurrentTime, 1199, 1201);
        output.Verify(x => x.Play(), Times.Exactly(2));
        await timedTask.StopAsync();
    }

    [Fact]
    public async Task DeviceChangeWhilePaused_ReconfiguresWithoutResuming()
    {
        var track = new AudioModel { Path = "paused-track.mp3" };
        using var stream = CreateWaveStream();

        var queue = new Mock<IPlaybackQueueService>();
        queue.Setup(x => x.GetCurrentTrack()).Returns(track);

        var loader = new Mock<ITrackLoader>();
        loader.Setup(x => x.Load(track)).Returns(TrackLoadResult.Success(stream));

        var output = new Mock<IPlaybackOutput>();
        output.SetupProperty(x => x.Volume, 1f);
        output
            .Setup(x => x.Reinitialize(It.IsAny<WaveStream>(), It.IsAny<int>()))
            .Returns(new PlaybackOutputReconfigureResult(true, false));

        var timedTask = new TimedTask();

        var player = new NAudioMusicPlayer(
            Mock.Of<ILogger>(),
            Mock.Of<IMediator>(),
            timedTask,
            queue.Object,
            loader.Object,
            output.Object,
            new PlaybackProgressMonitor());

        await player.PlayPauseAsync();
        await player.PlayPauseAsync();
        player.CurrentTime = 900;

        await player.Handle(new AudioOutputDeviceChangedNotification(new AudioOutputDevice(3, "Speakers")), CancellationToken.None);

        Assert.InRange(player.CurrentTime, 899, 901);
        output.Verify(x => x.Play(), Times.Once);
        output.Verify(x => x.Pause(), Times.Exactly(2));
        await timedTask.StopAsync();
    }

    [Fact]
    public async Task UnsupportedFormat_RemovesTrackAndRetriesCurrent()
    {
        var unsupported = new AudioModel { Path = "unsupported.wav" };
        var fallback = new AudioModel { Path = "fallback.mp3" };
        using var fallbackStream = CreateWaveStream();

        var queue = new Mock<IPlaybackQueueService>();
        queue.SetupSequence(x => x.GetCurrentTrack())
            .Returns(unsupported)
            .Returns(fallback);

        var loader = new Mock<ITrackLoader>();
        loader.Setup(x => x.Load(unsupported)).Returns(new TrackLoadResult(TrackLoadStatus.UnsupportedFormat, Reason: "Extensible"));
        loader.Setup(x => x.Load(fallback)).Returns(TrackLoadResult.Success(fallbackStream));

        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Publish(It.IsAny<CurrentSongNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var output = new Mock<IPlaybackOutput>();
        output.Setup(x => x.Reinitialize(It.IsAny<WaveStream>(), It.IsAny<int>()))
            .Returns(new PlaybackOutputReconfigureResult(true, false));

        var timedTask = new TimedTask();

        var player = new NAudioMusicPlayer(
            Mock.Of<ILogger>(),
            mediator.Object,
            timedTask,
            queue.Object,
            loader.Object,
            output.Object,
            new PlaybackProgressMonitor());

        await player.PlayPauseAsync();

        queue.Verify(x => x.RemoveTrack(unsupported), Times.Once);
        mediator.Verify(x => x.Publish(It.Is<CurrentSongNotification>(n => n.Audio == fallback), It.IsAny<CancellationToken>()), Times.Once);
        await timedTask.StopAsync();
    }

    [Fact]
    public async Task MissingFile_RemovesTrackAndRetriesCurrent()
    {
        var missing = new AudioModel { Path = "missing.mp3" };
        var fallback = new AudioModel { Path = "next.mp3" };
        using var fallbackStream = CreateWaveStream();

        var queue = new Mock<IPlaybackQueueService>();
        queue.SetupSequence(x => x.GetCurrentTrack())
            .Returns(missing)
            .Returns(fallback);

        var loader = new Mock<ITrackLoader>();
        loader.Setup(x => x.Load(missing)).Returns(new TrackLoadResult(TrackLoadStatus.MissingFile));
        loader.Setup(x => x.Load(fallback)).Returns(TrackLoadResult.Success(fallbackStream));

        var output = new Mock<IPlaybackOutput>();
        output.Setup(x => x.Reinitialize(It.IsAny<WaveStream>(), It.IsAny<int>()))
            .Returns(new PlaybackOutputReconfigureResult(true, false));

        var timedTask = new TimedTask();

        var player = new NAudioMusicPlayer(
            Mock.Of<ILogger>(),
            Mock.Of<IMediator>(),
            timedTask,
            queue.Object,
            loader.Object,
            output.Object,
            new PlaybackProgressMonitor());

        await player.PlayPauseAsync();

        queue.Verify(x => x.RemoveTrack(missing), Times.Once);
        await timedTask.StopAsync();
    }

    [Fact]
    public async Task JumpToCurrentIndexWhilePlaying_DoesNotRestartTrack()
    {
        var current = new AudioModel { Path = "current.mp3" };
        using var currentStream = CreateWaveStream();

        var queue = new Mock<IPlaybackQueueService>();
        queue.Setup(x => x.GetCurrentTrack()).Returns(current);
        queue.Setup(x => x.GetTrackAtIndex(0)).Returns(current);

        var loader = new Mock<ITrackLoader>();
        loader.Setup(x => x.Load(current)).Returns(TrackLoadResult.Success(currentStream));

        var output = new Mock<IPlaybackOutput>();
        output.Setup(x => x.Reinitialize(It.IsAny<WaveStream>(), It.IsAny<int>()))
            .Returns(new PlaybackOutputReconfigureResult(true, false));

        var timedTask = new TimedTask();

        var player = new NAudioMusicPlayer(
            Mock.Of<ILogger>(),
            Mock.Of<IMediator>(),
            timedTask,
            queue.Object,
            loader.Object,
            output.Object,
            new PlaybackProgressMonitor());

        await player.PlayPauseAsync();
        player.CurrentTime = 1234;

        await player.JumpToIndexAsync(0);

        Assert.InRange(player.CurrentTime, 1233, 1235);
        loader.Verify(x => x.Load(current), Times.Once);
        await timedTask.StopAsync();
    }

    [Fact]
    public void EndOfTrackMonitor_OnlyAdvancesAtBoundary()
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

    private static WaveStream CreateWaveStream(int durationMs = 5_000)
    {
        var format = new WaveFormat(44100, 16, 2);
        var bytesPerMillisecond = format.AverageBytesPerSecond / 1000;
        var buffer = new byte[Math.Max(bytesPerMillisecond * durationMs, format.BlockAlign)];
        return new RawSourceWaveStream(new MemoryStream(buffer), format);
    }
}
