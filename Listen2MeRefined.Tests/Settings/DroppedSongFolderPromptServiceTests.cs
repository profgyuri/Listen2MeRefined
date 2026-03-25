using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.ViewModels.Popups;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.WPF.Utils;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Settings;

public sealed class DroppedSongFolderPromptServiceTests
{
    [Fact]
    public async Task PromptAsync_WhenPopupConfirmed_ReturnsAddFolder()
    {
        SongDroppedPopupViewModel? configuredPopup = null;
        var windowManager = new Mock<IWindowManager>();
        windowManager
            .Setup(x => x.ShowPopupAsync<SongDroppedPopupViewModel>(
                It.Is<WindowShowOptions>(o => o.CentreOnMainWindow && o.IsModal),
                It.IsAny<Action<SongDroppedPopupViewModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                WindowShowOptions _,
                Action<SongDroppedPopupViewModel>? configure,
                CancellationToken _) =>
            {
                configuredPopup = CreatePopupViewModel();
                configure?.Invoke(configuredPopup);
                return (bool?)true;
            });

        var sut = new DroppedSongFolderPromptService(windowManager.Object);

        var decision = await sut.PromptAsync(@"C:\Music\SongFolder");

        Assert.Equal(AddDroppedSongFolderDecision.AddFolder, decision);
        Assert.NotNull(configuredPopup);
        Assert.Equal(@"C:\Music\SongFolder", configuredPopup!.FolderPath);
    }

    [Fact]
    public async Task PromptAsync_WhenPopupCanceledWithDontAskAgain_ReturnsSkipAndDontAskAgain()
    {
        var windowManager = new Mock<IWindowManager>();
        windowManager
            .Setup(x => x.ShowPopupAsync<SongDroppedPopupViewModel>(
                It.IsAny<WindowShowOptions>(),
                It.IsAny<Action<SongDroppedPopupViewModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                WindowShowOptions _,
                Action<SongDroppedPopupViewModel>? configure,
                CancellationToken _) =>
            {
                var popup = CreatePopupViewModel();
                configure?.Invoke(popup);
                popup.DontAskAgain = true;
                return (bool?)false;
            });

        var sut = new DroppedSongFolderPromptService(windowManager.Object);

        var decision = await sut.PromptAsync(@"C:\Music\SongFolder");

        Assert.Equal(AddDroppedSongFolderDecision.SkipAndDontAskAgain, decision);
    }

    [Fact]
    public async Task PromptAsync_WhenPopupCanceledWithoutDontAskAgain_ReturnsSkip()
    {
        var windowManager = new Mock<IWindowManager>();
        windowManager
            .Setup(x => x.ShowPopupAsync<SongDroppedPopupViewModel>(
                It.IsAny<WindowShowOptions>(),
                It.IsAny<Action<SongDroppedPopupViewModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                WindowShowOptions _,
                Action<SongDroppedPopupViewModel>? configure,
                CancellationToken _) =>
            {
                var popup = CreatePopupViewModel();
                configure?.Invoke(popup);
                popup.DontAskAgain = false;
                return (bool?)false;
            });

        var sut = new DroppedSongFolderPromptService(windowManager.Object);

        var decision = await sut.PromptAsync(@"C:\Music\SongFolder");

        Assert.Equal(AddDroppedSongFolderDecision.Skip, decision);
    }

    private static SongDroppedPopupViewModel CreatePopupViewModel()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        return new SongDroppedPopupViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            new WeakReferenceMessenger());
    }
}
