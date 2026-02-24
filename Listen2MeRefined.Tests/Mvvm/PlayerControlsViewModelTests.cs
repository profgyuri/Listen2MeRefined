using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.Infrastructure.Settings.Playback;
using Listen2MeRefined.Infrastructure.Utils;
using Moq;
using Serilog;
using SkiaSharp;
using PlayerControlsViewModel = Listen2MeRefined.Infrastructure.ViewModels.MainWindow.PlayerControlsViewModel;

namespace Listen2MeRefined.Tests.Mvvm;

public class PlayerControlsViewModelTests
{
    [Fact]
    public async Task InitializeAsync_AppliesConfiguredStartupVolume_WhenNotMuted()
    {
        var settings = new AppSettings
        {
            StartupVolume = 0.42f,
            StartMuted = false
        };
        var (viewModel, musicPlayer, timedTask, _) = await CreateViewModelAsync(settings);

        Assert.InRange(musicPlayer.Object.Volume, 0.419f, 0.421f);
        Assert.False(viewModel.IsMuted);

        await timedTask.StopAsync();
    }

    [Fact]
    public async Task InitializeAsync_StartsMuted_WhenConfigured()
    {
        var settings = new AppSettings
        {
            StartupVolume = 0.75f,
            StartMuted = true
        };
        var (viewModel, musicPlayer, timedTask, _) = await CreateViewModelAsync(settings);

        Assert.InRange(musicPlayer.Object.Volume, -0.001f, 0.001f);
        Assert.True(viewModel.IsMuted);

        await timedTask.StopAsync();
    }

    [Fact]
    public async Task VolumeChange_PersistsStartupVolumeAndUnmutes()
    {
        var settings = new AppSettings
        {
            StartupVolume = 0.2f,
            StartMuted = true
        };
        var (viewModel, musicPlayer, timedTask, _) = await CreateViewModelAsync(settings);

        viewModel.Volume = 0.63f;

        Assert.InRange(musicPlayer.Object.Volume, 0.629f, 0.631f);
        Assert.InRange(settings.StartupVolume, 0.629f, 0.631f);
        Assert.False(settings.StartMuted);
        Assert.False(viewModel.IsMuted);

        await timedTask.StopAsync();
    }

    [Fact]
    public async Task UpdateWaveformViewport_ValidSize_UpdatesDimensionsAndResizesDrawer()
    {
        var settings = new AppSettings();
        var (viewModel, _, timedTask, waveFormDrawer) = await CreateViewModelAsync(settings, waveformResizeDebounce: TimeSpan.Zero);

        viewModel.UpdateWaveformViewport(612.4, 132.8);
        await viewModel.WaitForPendingWaveformRedrawAsync();

        Assert.Equal(612, viewModel.WaveFormWidth);
        Assert.Equal(133, viewModel.WaveFormHeight);
        waveFormDrawer.Verify(x => x.SetSize(612, 133), Times.Once);

        await timedTask.StopAsync();
    }

    [Fact]
    public async Task UpdateWaveformViewport_WithZeroSize_DoesNotChangeDimensions()
    {
        var settings = new AppSettings();
        var (viewModel, _, timedTask, waveFormDrawer) = await CreateViewModelAsync(settings, waveformResizeDebounce: TimeSpan.Zero);
        var initialWidth = viewModel.WaveFormWidth;
        var initialHeight = viewModel.WaveFormHeight;

        viewModel.UpdateWaveformViewport(0, 0);

        Assert.Equal(initialWidth, viewModel.WaveFormWidth);
        Assert.Equal(initialHeight, viewModel.WaveFormHeight);
        waveFormDrawer.Verify(x => x.SetSize(It.IsAny<int>(), It.IsAny<int>()), Times.Once);

        await timedTask.StopAsync();
    }

    [Fact]
    public async Task UpdateWaveformViewport_TinySize_ClampsToMinimumDimensions()
    {
        var settings = new AppSettings();
        var (viewModel, _, timedTask, waveFormDrawer) = await CreateViewModelAsync(settings, waveformResizeDebounce: TimeSpan.Zero);

        viewModel.UpdateWaveformViewport(1, 1);
        await viewModel.WaitForPendingWaveformRedrawAsync();

        Assert.Equal(64, viewModel.WaveFormWidth);
        Assert.Equal(24, viewModel.WaveFormHeight);
        waveFormDrawer.Verify(x => x.SetSize(64, 24), Times.Once);

        await timedTask.StopAsync();
    }

    [Fact]
    public async Task UpdateWaveformViewport_WithoutCurrentTrack_RedrawsPlaceholderLine()
    {
        var settings = new AppSettings();
        var (viewModel, _, timedTask, waveFormDrawer) = await CreateViewModelAsync(settings, waveformResizeDebounce: TimeSpan.Zero);
        var initialLineCallCount = waveFormDrawer.Invocations.Count(x => x.Method.Name == nameof(IWaveFormDrawer<SKBitmap>.LineAsync));

        viewModel.UpdateWaveformViewport(520, 92);
        await viewModel.WaitForPendingWaveformRedrawAsync();

        var updatedLineCallCount = waveFormDrawer.Invocations.Count(x => x.Method.Name == nameof(IWaveFormDrawer<SKBitmap>.LineAsync));
        Assert.True(updatedLineCallCount > initialLineCallCount);

        await timedTask.StopAsync();
    }

    [Fact]
    public async Task UpdateWaveformViewport_WithCurrentTrack_RedrawsTrackWaveform()
    {
        var settings = new AppSettings();
        var (viewModel, _, timedTask, waveFormDrawer) = await CreateViewModelAsync(settings, waveformResizeDebounce: TimeSpan.Zero);

        var audio = new AudioModel
        {
            Path = "E:\\music\\track.mp3",
            Length = TimeSpan.FromSeconds(90)
        };

        await viewModel.Handle(new CurrentSongNotification(audio), CancellationToken.None);
        var initialWaveformCallCount = waveFormDrawer.Invocations.Count(x => x.Method.Name == nameof(IWaveFormDrawer<SKBitmap>.WaveFormAsync));

        viewModel.UpdateWaveformViewport(740, 130);
        await viewModel.WaitForPendingWaveformRedrawAsync();

        var updatedWaveformCallCount = waveFormDrawer.Invocations.Count(x => x.Method.Name == nameof(IWaveFormDrawer<SKBitmap>.WaveFormAsync));
        Assert.True(updatedWaveformCallCount > initialWaveformCallCount);

        await timedTask.StopAsync();
    }

    private static async Task<(PlayerControlsViewModel ViewModel, Mock<IMusicPlayerController> MusicPlayer, TimedTask TimedTask, Mock<IWaveFormDrawer<SKBitmap>> WaveFormDrawer)> CreateViewModelAsync(
        AppSettings settings,
        TimeSpan? waveformResizeDebounce = null)
    {
        var logger = Mock.Of<ILogger>();
        var waveFormDrawer = new Mock<IWaveFormDrawer<SKBitmap>>();
        waveFormDrawer.Setup(x => x.LineAsync()).ReturnsAsync(new SKBitmap(1, 1));
        waveFormDrawer.Setup(x => x.WaveFormAsync(It.IsAny<string>())).ReturnsAsync(new SKBitmap(1, 1));

        var musicPlayer = new Mock<IMusicPlayerController>();
        musicPlayer.SetupProperty(x => x.Volume, 1f);

        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);
        settingsManager
            .Setup(x => x.SaveSettings(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(action => action(settings));

        var timedTask = new TimedTask();
        var playbackDefaultsService = new PlaybackDefaultsService(settingsManager.Object);
        var viewModel = new PlayerControlsViewModel(
            logger,
            waveFormDrawer.Object,
            musicPlayer.Object,
            playbackDefaultsService,
            timedTask,
            waveformResizeDebounce: waveformResizeDebounce);

        await viewModel.InitializeAsync();
        return (viewModel, musicPlayer, timedTask, waveFormDrawer);
    }
}
