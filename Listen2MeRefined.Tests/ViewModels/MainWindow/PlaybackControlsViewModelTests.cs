using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels.MainWindow;

public class PlaybackControlsViewModelTests
{
    private static readonly IUiDispatcher UiDispatcher = new ImmediateUiDispatcher();

    [Fact]
    public async Task InitializeAsync_LoadsFontFamilyFromSettings()
    {
        var (viewModel, _, timedTask, _) = await CreateViewModelAsync("Consolas");

        Assert.Equal("Consolas", viewModel.FontFamilyName);

        await timedTask.StopAsync();
    }

    [Fact]
    public async Task CurrentSongChangedMessage_UpdatesTotalTimeDisplay()
    {
        var (viewModel, _, timedTask, messenger) = await CreateViewModelAsync();

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
        var (viewModel, musicPlayer, timedTask, _) = await CreateViewModelAsync();

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
        string fontFamily = "Segoe UI")
    {
        var logger = Mock.Of<ILogger>();

        var musicPlayer = new Mock<IMusicPlayerController>();
        musicPlayer.SetupProperty(x => x.Volume, 1f);
        musicPlayer.SetupProperty(x => x.RepeatMode, RepeatMode.Off);

        var messenger = new WeakReferenceMessenger();
        var timedTask = new TimedTask();
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetFontFamily()).Returns(fontFamily);
        var viewModel = new PlaybackControlsViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
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
