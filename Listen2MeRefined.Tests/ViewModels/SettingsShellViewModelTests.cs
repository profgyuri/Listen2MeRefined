using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.ViewModels.SettingsTabs;
using Listen2MeRefined.Application.ViewModels.Shells;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class SettingsShellViewModelTests
{
    [Fact]
    public async Task InitializeAsync_State_NavigatesToGeneralRoute()
    {
        var (viewModel, navigationService, _, _) = CreateViewModel();

        await viewModel.InitializeAsync();

        navigationService.Verify(
            x => x.NavigateAsync<SettingsGeneralTabViewModel>(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_State_UsesNavigationItemsFromProvider()
    {
        var customItems = new[]
        {
            new SettingsShellNavigationItem("settings/customA", "Custom A", "Tune"),
            new SettingsShellNavigationItem("settings/customB", "Custom B", "Tune")
        };
        var (viewModel, _, _, _) = CreateViewModel(customItems);

        Assert.Equal(customItems, viewModel.NavigationItems);
    }

    [Fact]
    public void CurrentRoute_State_UpdatesActiveNavigationItem()
    {
        var customItems = new[]
        {
            new SettingsShellNavigationItem("settings/general", "General", "Tune"),
            new SettingsShellNavigationItem("settings/library", "Library", "FolderMusicOutline")
        };
        var (viewModel, _, navigationState, _) = CreateViewModel(customItems);

        navigationState.CurrentRoute = "settings/library";

        Assert.All(
            viewModel.NavigationItems,
            item => Assert.Equal(item.Route == "settings/library", item.IsActive));
    }

    private static (
        SettingsShellViewModel ViewModel,
        Mock<INavigationService> NavigationService,
        NavigationState NavigationState,
        Mock<ISettingsShellNavigationProvider> NavigationProvider) CreateViewModel(
            IReadOnlyList<SettingsShellNavigationItem>? navigationItems = null)
    {
        var navigationService = new Mock<INavigationService>();
        var navigationState = new NavigationState();
        var shellContext = new ShellContext(
            navigationState,
            navigationService.Object,
            Mock.Of<IInitializationTracker>());

        var shellContextFactory = new Mock<IShellContextFactory>();
        shellContextFactory
            .Setup(x => x.Create())
            .Returns(shellContext);

        var provider = new Mock<ISettingsShellNavigationProvider>();
        provider
            .Setup(x => x.CreateNavigationItems())
            .Returns(navigationItems ?? new[]
            {
                new SettingsShellNavigationItem("settings/general", "General", "Tune")
            });

        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        var viewModel = new SettingsShellViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            Mock.Of<IMessenger>(),
            shellContextFactory.Object,
            provider.Object);

        return (viewModel, navigationService, navigationState, provider);
    }
}
