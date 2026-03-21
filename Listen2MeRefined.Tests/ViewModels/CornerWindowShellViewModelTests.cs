using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.Shells;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class CornerWindowShellViewModelTests
{
    [Fact]
    public async Task InitializeAsync_State_NavigatesToCornerHomeRoute()
    {
        var (viewModel, navigationService) = CreateViewModel();

        await viewModel.InitializeAsync();

        navigationService.Verify(
            x => x.NavigateAsync("corner/home", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static (CornerWindowShellViewModel ViewModel, Mock<INavigationService> NavigationService) CreateViewModel()
    {
        var navigationService = new Mock<INavigationService>();
        var shellContext = new ShellContext(
            new NavigationState(),
            navigationService.Object,
            Mock.Of<IInitializationTracker>());

        var shellContextFactory = new Mock<IShellContextFactory>();
        shellContextFactory
            .Setup(x => x.Create())
            .Returns(shellContext);

        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        var viewModel = new CornerWindowShellViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            Mock.Of<IMessenger>(),
            shellContextFactory.Object);

        return (viewModel, navigationService);
    }
}
