using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels.MainWindow;

public class PlaybackControlsViewModelTests
{
    private static readonly IUiDispatcher UiDispatcher = new ImmediateUiDispatcher();

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
    public async Task CurrentSongChangedMessage_UpdatesTotalTimeDisplay()
    {
        var settings = new AppSettings();
        var (viewModel, _, timedTask, messenger) = await CreateViewModelAsync(settings);

        var audio = new AudioModel
        {
            Path = "E:\\music\\track.mp3",
            Length = TimeSpan.FromSeconds(90)
        };

        messenger.Send(new CurrentSongChangedMessage(audio));
        await Task.Delay(50);

        Assert.Equal(TimeSpan.FromSeconds(90), viewModel.TotalTimeDisplay);

        await timedTask.StopAsync();
    }

    [Fact]
    public async Task ToggleRepeatCommand_CyclesOffAllOne()
    {
        var settings = new AppSettings();
        var (viewModel, musicPlayer, timedTask, _) = await CreateViewModelAsync(settings);

        Assert.Equal(RepeatMode.Off, viewModel.RepeatMode);
        Assert.False(viewModel.IsRepeatActive);

        viewModel.ToggleRepeatCommand.Execute(null);
        Assert.Equal(RepeatMode.All, viewModel.RepeatMode);
        Assert.True(viewModel.IsRepeatActive);
        Assert.Equal("Repeat", viewModel.RepeatIconKind);

        viewModel.ToggleRepeatCommand.Execute(null);
        Assert.Equal(RepeatMode.One, viewModel.RepeatMode);
        Assert.True(viewModel.IsRepeatActive);
        Assert.Equal("RepeatOnce", viewModel.RepeatIconKind);

        viewModel.ToggleRepeatCommand.Execute(null);
        Assert.Equal(RepeatMode.Off, viewModel.RepeatMode);
        Assert.False(viewModel.IsRepeatActive);
        Assert.Equal("Repeat", viewModel.RepeatIconKind);

        await timedTask.StopAsync();
    }

    private static async Task<(PlaybackControlsViewModel ViewModel, Mock<IMusicPlayerController> MusicPlayer, TimedTask TimedTask, WeakReferenceMessenger Messenger)> CreateViewModelAsync(
        AppSettings settings)
    {
        var logger = Mock.Of<ILogger>();

        var musicPlayer = new Mock<IMusicPlayerController>();
        musicPlayer.SetupProperty(x => x.Volume, 1f);
        musicPlayer.SetupProperty(x => x.RepeatMode, RepeatMode.Off);

        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);
        settingsManager
            .Setup(x => x.SaveSettings(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(action => action(settings));

        var messenger = new WeakReferenceMessenger();
        var timedTask = new TimedTask();
        var playbackDefaultsService = new PlaybackDefaultsService(settingsManager.Object);
        var playbackVolumeSetter = new PlaybackVolumeSetter(musicPlayer.Object, playbackDefaultsService);
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetFontFamily()).Returns("Segoe UI");
        var viewModel = new PlaybackControlsViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            playbackVolumeSetter,
            musicPlayer.Object,
            settingsReader.Object,
            UiDispatcher,
            timedTask);

        await viewModel.InitializeAsync();
        return (viewModel, musicPlayer, timedTask, messenger);
    }

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;

        public Task InvokeAsync(Action action, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(action);
            ct.ThrowIfCancellationRequested();
            action();
            return Task.CompletedTask;
        }

        public Task InvokeAsync(Func<Task> func, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(func);
            ct.ThrowIfCancellationRequested();
            return func();
        }

        public Task<T> InvokeAsync<T>(Func<T> func, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(func);
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(func());
        }
    }
}
