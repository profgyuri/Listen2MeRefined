using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels.MainWindow;

public sealed class NowPlayingVolumeViewModelTests
{
    [Fact]
    public async Task InitializeAsync_AppliesConfiguredStartupVolume_WhenNotMuted()
    {
        var settings = new AppSettings
        {
            StartupVolume = 0.42f,
            StartMuted = false
        };

        var (viewModel, musicPlayer, _) = await CreateViewModelAsync(settings);

        Assert.InRange(musicPlayer.Object.Volume, 0.419f, 0.421f);
        Assert.False(viewModel.IsMuted);
        Assert.Equal("VolumeMedium", viewModel.VolumeIconKind);
    }

    [Fact]
    public async Task InitializeAsync_StartsMuted_WhenConfigured()
    {
        var settings = new AppSettings
        {
            StartupVolume = 0.75f,
            StartMuted = true
        };

        var (viewModel, musicPlayer, _) = await CreateViewModelAsync(settings);

        Assert.InRange(musicPlayer.Object.Volume, -0.001f, 0.001f);
        Assert.True(viewModel.IsMuted);
        Assert.Equal("VolumeOff", viewModel.VolumeIconKind);
    }

    [Fact]
    public async Task VolumeChange_PersistsStartupVolumeAndUnmutes()
    {
        var settings = new AppSettings
        {
            StartupVolume = 0.2f,
            StartMuted = true
        };

        var (viewModel, musicPlayer, persistedSettings) = await CreateViewModelAsync(settings);

        viewModel.Volume = 0.63f;

        Assert.InRange(musicPlayer.Object.Volume, 0.629f, 0.631f);
        Assert.InRange(persistedSettings.StartupVolume, 0.629f, 0.631f);
        Assert.False(persistedSettings.StartMuted);
        Assert.False(viewModel.IsMuted);
        Assert.Equal("VolumeMedium", viewModel.VolumeIconKind);
    }

    [Fact]
    public async Task ToggleMuteCommand_TransitionsMuteStateAndIcon()
    {
        var settings = new AppSettings
        {
            StartupVolume = 0.8f,
            StartMuted = false
        };

        var (viewModel, musicPlayer, persistedSettings) = await CreateViewModelAsync(settings);

        Assert.Equal("VolumeHigh", viewModel.VolumeIconKind);
        Assert.False(viewModel.IsMuted);

        await viewModel.ToggleMuteCommand.ExecuteAsync(null);

        Assert.InRange(musicPlayer.Object.Volume, -0.001f, 0.001f);
        Assert.True(viewModel.IsMuted);
        Assert.Equal("VolumeOff", viewModel.VolumeIconKind);
        Assert.True(persistedSettings.StartMuted);

        await viewModel.ToggleMuteCommand.ExecuteAsync(null);

        Assert.InRange(musicPlayer.Object.Volume, 0.799f, 0.801f);
        Assert.False(viewModel.IsMuted);
        Assert.Equal("VolumeHigh", viewModel.VolumeIconKind);
        Assert.False(persistedSettings.StartMuted);
    }

    private static async Task<(NowPlayingVolumeViewModel ViewModel, Mock<IMusicPlayerController> MusicPlayer, AppSettings Settings)> CreateViewModelAsync(
        AppSettings settings)
    {
        var logger = Mock.Of<ILogger>();
        var messenger = new WeakReferenceMessenger();

        var musicPlayer = new Mock<IMusicPlayerController>();
        musicPlayer.SetupProperty(x => x.Volume, 1f);
        musicPlayer.SetupProperty(x => x.RepeatMode, RepeatMode.Off);

        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);
        settingsManager
            .Setup(x => x.SaveSettings(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(save => save(settings));

        var playbackDefaultsService = new PlaybackDefaultsService(settingsManager.Object);
        var playbackVolumeSetter = new PlaybackVolumeSetter(musicPlayer.Object, playbackDefaultsService);

        var viewModel = new NowPlayingVolumeViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            playbackVolumeSetter,
            musicPlayer.Object);

        await viewModel.InitializeAsync();
        return (viewModel, musicPlayer, settings);
    }
}
