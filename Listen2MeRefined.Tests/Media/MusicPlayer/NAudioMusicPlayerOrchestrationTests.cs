using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Moq;
using NAudio.Wave;
using Serilog;

namespace Listen2MeRefined.Tests.Media.MusicPlayer;

public class NAudioMusicPlayerOrchestrationTests
{
    [Fact]
    public async Task StartupOutputDeviceConfiguration_State_UsesPersistedDeviceOnFirstPlayback()
    {
        var track = new AudioModel { Path = "startup-device-track.mp3" };
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
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetAudioOutputDeviceName()).Returns("Speakers");

        var outputDevice = new Mock<IOutputDevice>();
        outputDevice
            .Setup(x => x.EnumerateOutputDevices())
            .Returns([new AudioOutputDevice(-1, "Windows Default"), new AudioOutputDevice(1, "Speakers")]);

        var player = new NAudioMusicPlayer(
            Mock.Of<ILogger>(),
            timedTask,
            queue.Object,
            loader.Object,
            output.Object,
            new PlaybackProgressMonitor(),
            settingsReader.Object,
            outputDevice.Object,
            new WeakReferenceMessenger());

        await player.PlayPauseAsync();

        output.Verify(x => x.Reinitialize(It.IsAny<WaveStream>(), 1), Times.Once);
        await timedTask.StopAsync();
    }

    [Fact]
    public async Task AudioOutputDeviceChangedMessage_WhilePlaying_ReconfiguresAndResumesWithTimestamp()
    {
        var track = new AudioModel { Path = "message-device-track.mp3" };
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
        var messenger = new WeakReferenceMessenger();

        var player = new NAudioMusicPlayer(
            Mock.Of<ILogger>(),
            timedTask,
            queue.Object,
            loader.Object,
            output.Object,
            new PlaybackProgressMonitor(),
            CreateSettingsReader(),
            CreateOutputDevice(),
            messenger);

        await player.PlayPauseAsync();
        player.CurrentTime = 1200;

        messenger.Send(new AudioOutputDeviceChangedMessage(new AudioOutputDevice(7, "USB DAC")));

        for (var i = 0; i < 20; i++)
        {
            if (output.Invocations.Any(x => x.Method.Name == nameof(IPlaybackOutput.Reinitialize) && (int)x.Arguments[1] == 7))
            {
                break;
            }

            await Task.Delay(25);
        }

        Assert.InRange(player.CurrentTime, 1199, 1201);
        output.Verify(x => x.Reinitialize(It.IsAny<WaveStream>(), 7), Times.AtLeastOnce);
        output.Verify(x => x.Play(), Times.Exactly(2));
        await timedTask.StopAsync();
    }

    [Fact]
    public async Task DeviceChangeWhilePlaying_ReconfiguresAndResumesWithTimestamp()
    {
        var track = new AudioModel { Path = "playing-track.mp3" };
        using var stream = CreateWaveStream();

        var logger = Mock.Of<ILogger>();
        var messenger = new WeakReferenceMessenger();

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
            timedTask,
            queue.Object,
            loader.Object,
            output.Object,
            new PlaybackProgressMonitor(),
            CreateSettingsReader(),
            CreateOutputDevice(),
            messenger);

        await player.PlayPauseAsync();
        player.CurrentTime = 1200;

        messenger.Send(new AudioOutputDeviceChangedMessage(new AudioOutputDevice(2, "Headphones")));

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
        var messenger = new WeakReferenceMessenger();

        var player = new NAudioMusicPlayer(
            Mock.Of<ILogger>(),
            timedTask,
            queue.Object,
            loader.Object,
            output.Object,
            new PlaybackProgressMonitor(),
            CreateSettingsReader(),
            CreateOutputDevice(),
            messenger);

        await player.PlayPauseAsync();
        await player.PlayPauseAsync();
        player.CurrentTime = 900;

        messenger.Send(new AudioOutputDeviceChangedMessage(new AudioOutputDevice(3, "Speakers")));

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

        var messenger = new WeakReferenceMessenger();
        var probe = new CurrentSongProbe();
        messenger.Register<CurrentSongProbe, CurrentSongChangedMessage>(
            probe,
            static (recipient, message) => recipient.Song = message.Value);

        var output = new Mock<IPlaybackOutput>();
        output.Setup(x => x.Reinitialize(It.IsAny<WaveStream>(), It.IsAny<int>()))
            .Returns(new PlaybackOutputReconfigureResult(true, false));

        var timedTask = new TimedTask();

        var player = new NAudioMusicPlayer(
            Mock.Of<ILogger>(),
            timedTask,
            queue.Object,
            loader.Object,
            output.Object,
            new PlaybackProgressMonitor(),
            CreateSettingsReader(),
            CreateOutputDevice(),
            messenger);

        await player.PlayPauseAsync();

        queue.Verify(x => x.RemoveTrack(unsupported), Times.Once);
        Assert.Same(fallback, probe.Song);
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
            timedTask,
            queue.Object,
            loader.Object,
            output.Object,
            new PlaybackProgressMonitor(),
            CreateSettingsReader(),
            CreateOutputDevice(),
            new WeakReferenceMessenger());

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
            timedTask,
            queue.Object,
            loader.Object,
            output.Object,
            new PlaybackProgressMonitor(),
            CreateSettingsReader(),
            CreateOutputDevice(),
            new WeakReferenceMessenger());

        await player.PlayPauseAsync();
        player.CurrentTime = 1234;

        await player.JumpToIndexAsync(0);

        Assert.InRange(player.CurrentTime, 1233, 1235);
        loader.Verify(x => x.Load(current), Times.Once);
        await timedTask.StopAsync();
    }

    private static WaveStream CreateWaveStream(int durationMs = 5_000)
    {
        var format = new WaveFormat(44100, 16, 2);
        var bytesPerMillisecond = format.AverageBytesPerSecond / 1000;
        var buffer = new byte[Math.Max(bytesPerMillisecond * durationMs, format.BlockAlign)];
        return new RawSourceWaveStream(new MemoryStream(buffer), format);
    }

    private static IAppSettingsReader CreateSettingsReader()
    {
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetAudioOutputDeviceName()).Returns("Windows Default");
        return settingsReader.Object;
    }

    private static IOutputDevice CreateOutputDevice()
    {
        var outputDevice = new Mock<IOutputDevice>();
        outputDevice
            .Setup(x => x.EnumerateOutputDevices())
            .Returns([new AudioOutputDevice(-1, "Windows Default"), new AudioOutputDevice(1, "Speakers")]);
        return outputDevice.Object;
    }

    private sealed class CurrentSongProbe
    {
        public AudioModel? Song { get; set; }
    }
}
