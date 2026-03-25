using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.Models;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class CornerShellDefaultHomeViewModelTests
{
    [Fact]
    public async Task InitializeAsync_State_LoadsPersistedValuesAndInitializesTrackInfoViewModel()
    {
        var (viewModel, _, trackInfoViewModel, _) = CreateViewModel(
            fontFamily: "Consolas",
            initialWindowPosition: "Always on top");

        await viewModel.InitializeAsync();

        Assert.Equal("Consolas", viewModel.FontFamilyName);
        Assert.True(viewModel.IsTopmost);
        Assert.True(trackInfoViewModel.IsInitialized);
    }

    [Fact]
    public async Task FontFamilyChangedMessage_State_UpdatesFontFamilyName()
    {
        var (viewModel, messenger, _, _) = CreateViewModel();
        await viewModel.InitializeAsync();

        messenger.Send(new FontFamilyChangedMessage("Courier New"));

        Assert.Equal("Courier New", viewModel.FontFamilyName);
    }

    [Fact]
    public async Task CornerWindowPositionChangedMessage_State_UpdatesIsTopmost()
    {
        var (viewModel, messenger, _, windowPositionPolicyService) = CreateViewModel(
            initialWindowPosition: "Default");
        await viewModel.InitializeAsync();

        messenger.Send(new CornerWindowPositionChangedMessage("Always on top"));

        Assert.True(viewModel.IsTopmost);
        windowPositionPolicyService.Verify(x => x.IsTopmost("Always on top"), Times.Once);
    }

    [Fact]
    public async Task CurrentSongChangedMessage_State_UpdatesHostedTrackInfoViewModel()
    {
        var (viewModel, messenger, trackInfoViewModel, _) = CreateViewModel();
        await viewModel.InitializeAsync();

        var song = new AudioModel
        {
            Artist = "Artist",
            Title = "Title",
            Genre = "Genre",
            Path = "song.mp3"
        };

        messenger.Send(new CurrentSongChangedMessage(song));

        Assert.Same(song, trackInfoViewModel.Song);
    }

    private static (
        CornerShellDefaultHomeViewModel ViewModel,
        WeakReferenceMessenger Messenger,
        TrackInfoViewModel TrackInfoViewModel,
        Mock<IWindowPositionPolicyService> WindowPositionPolicyService) CreateViewModel(
        string fontFamily = "Segoe UI",
        string initialWindowPosition = "Default")
    {
        var messenger = new WeakReferenceMessenger();
        var logger = CreateLogger();

        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetFontFamily()).Returns(fontFamily);
        settingsReader.Setup(x => x.GetNewSongWindowPosition()).Returns(initialWindowPosition);

        var windowPositionPolicyService = new Mock<IWindowPositionPolicyService>();
        windowPositionPolicyService
            .Setup(x => x.IsTopmost(It.IsAny<string?>()))
            .Returns<string?>(position =>
                string.Equals(position, "Always on top", StringComparison.OrdinalIgnoreCase));

        var trackInfoViewModel = new TrackInfoViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            settingsReader.Object);

        var viewModel = new CornerShellDefaultHomeViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            trackInfoViewModel,
            settingsReader.Object,
            windowPositionPolicyService.Object);

        return (viewModel, messenger, trackInfoViewModel, windowPositionPolicyService);
    }

    private static Mock<ILogger> CreateLogger()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        return logger;
    }
}
