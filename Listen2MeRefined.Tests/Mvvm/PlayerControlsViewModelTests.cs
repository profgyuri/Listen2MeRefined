using Listen2MeRefined.Infrastructure;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using Listen2MeRefined.Infrastructure.Mvvm.MainWindow;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Storage;
using Moq;
using Serilog;
using SkiaSharp;

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
        var (viewModel, musicPlayer, timedTask) = await CreateViewModelAsync(settings);

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
        var (viewModel, musicPlayer, timedTask) = await CreateViewModelAsync(settings);

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
        var (viewModel, musicPlayer, timedTask) = await CreateViewModelAsync(settings);

        viewModel.Volume = 0.63f;

        Assert.InRange(musicPlayer.Object.Volume, 0.629f, 0.631f);
        Assert.InRange(settings.StartupVolume, 0.629f, 0.631f);
        Assert.False(settings.StartMuted);
        Assert.False(viewModel.IsMuted);

        await timedTask.StopAsync();
    }

    private static async Task<(PlayerControlsViewModel ViewModel, Mock<IMusicPlayerController> MusicPlayer, TimedTask TimedTask)> CreateViewModelAsync(AppSettings settings)
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
            timedTask);

        await viewModel.InitializeAsync();
        return (viewModel, musicPlayer, timedTask);
    }
}
