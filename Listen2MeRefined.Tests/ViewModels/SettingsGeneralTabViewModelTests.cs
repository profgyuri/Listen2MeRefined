using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Updating;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.SettingsTabs;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class SettingsGeneralTabViewModelTests
{
    [Fact]
    public async Task InitializeAsync_State_LoadsPersistedGeneralSettings()
    {
        var settings = new AppSettings
        {
            FontFamily = "Consolas",
            UseCompactPlaylistView = true,
            AutoCheckUpdatesOnStartup = false,
            ThemeMode = "Light",
            AccentColor = "Green"
        };
        var (viewModel, _, _, _, _, _) = CreateViewModel(settings);

        await viewModel.InitializeAsync();

        Assert.Equal("Consolas", viewModel.SelectedFontFamily);
        Assert.Equal("Compact", viewModel.SelectedPlaylistViewMode);
        Assert.False(viewModel.AutoCheckUpdatesOnStartup);
        Assert.Equal("Light", viewModel.SelectedThemeMode);
        Assert.Equal("Green", viewModel.SelectedAccentColor);
        Assert.Equal("Automatic update checks are disabled.", viewModel.UpdateAvailableText);
        Assert.False(viewModel.IsUpdateButtonVisible);
    }

    [Fact]
    public async Task SelectedFontFamily_State_PersistsAndPublishesMessage()
    {
        var settings = new AppSettings
        {
            FontFamily = "Segoe UI",
            AutoCheckUpdatesOnStartup = false
        };
        var (viewModel, _, _, _, _, probe) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        viewModel.SelectedFontFamily = "Consolas";

        Assert.Equal("Consolas", settings.FontFamily);
        Assert.Equal("Consolas", probe.FontFamily);
    }

    [Fact]
    public async Task SelectedPlaylistViewMode_State_PersistsAndPublishesMessage()
    {
        var settings = new AppSettings
        {
            UseCompactPlaylistView = false,
            AutoCheckUpdatesOnStartup = false
        };
        var (viewModel, _, _, _, _, probe) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        viewModel.SelectedPlaylistViewMode = "Compact";

        Assert.True(settings.UseCompactPlaylistView);
        Assert.True(probe.UseCompactPlaylistView);
    }

    [Fact]
    public async Task CheckForUpdatesNowCommand_State_HandlesExceptionsViaErrorHandler()
    {
        var settings = new AppSettings
        {
            AutoCheckUpdatesOnStartup = false
        };
        var (viewModel, _, _, _, errorHandler, _) = CreateViewModel(settings, shouldThrowOnUpdateCheck: true);
        await viewModel.InitializeAsync();

        await viewModel.CheckForUpdatesNowCommand.ExecuteAsync(null);

        errorHandler.Verify(
            x => x.HandleAsync(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static (
        SettingsGeneralTabViewModel ViewModel,
        AppSettings Settings,
        Mock<IAppThemeService> AppThemeService,
        Mock<IAppUpdateChecker> UpdateChecker,
        Mock<IErrorHandler> ErrorHandler,
        MessageProbe Probe) CreateViewModel(
            AppSettings settings,
            bool shouldThrowOnUpdateCheck = false)
    {
        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);
        settingsManager
            .Setup(x => x.SaveSettings(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(apply => apply(settings));

        var settingsReader = new AppSettingsReader(settingsManager.Object);
        var settingsWriter = new AppSettingsWriter(settingsManager.Object);

        var updateChecker = new Mock<IAppUpdateChecker>();
        if (shouldThrowOnUpdateCheck)
        {
            updateChecker
                .Setup(x => x.CheckForUpdatesAsync())
                .ThrowsAsync(new InvalidOperationException("Update checker unavailable"));
        }
        else
        {
            updateChecker
                .Setup(x => x.CheckForUpdatesAsync())
                .ReturnsAsync(new AppUpdateCheckResult(
                    false,
                    "You are using the latest version.",
                    false));
        }

        var appThemeService = new Mock<IAppThemeService>();
        appThemeService.Setup(x => x.GetThemeModes()).Returns(["Dark", "Light"]);
        appThemeService.Setup(x => x.GetAccentColors()).Returns(["Orange", "Green", "Blue"]);

        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        var errorHandler = new Mock<IErrorHandler>();
        errorHandler
            .Setup(x => x.HandleAsync(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var messenger = new WeakReferenceMessenger();
        var probe = new MessageProbe();
        messenger.Register<MessageProbe, FontFamilyChangedMessage>(
            probe,
            static (recipient, message) => recipient.FontFamily = message.Value);
        messenger.Register<MessageProbe, PlaylistViewModeChangedMessage>(
            probe,
            static (recipient, message) => recipient.UseCompactPlaylistView = message.Value);

        var viewModel = new SettingsGeneralTabViewModel(
            errorHandler.Object,
            logger.Object,
            messenger,
            new FontFamilies(["Segoe UI", "Consolas"]),
            settingsReader,
            settingsWriter,
            updateChecker.Object,
            Mock.Of<IVersionChecker>(),
            appThemeService.Object);

        return (viewModel, settings, appThemeService, updateChecker, errorHandler, probe);
    }

    private sealed class MessageProbe
    {
        public string? FontFamily { get; set; }

        public bool UseCompactPlaylistView { get; set; }
    }
}
