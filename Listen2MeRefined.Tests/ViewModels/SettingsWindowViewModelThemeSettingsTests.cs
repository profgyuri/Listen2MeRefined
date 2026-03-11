using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Folders;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Application.Updating;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Core.Repositories;
using Listen2MeRefined.Infrastructure.FolderBrowser;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.Infrastructure.Utils;
using MediatR;
using Moq;
using Serilog;
using SettingsWindowViewModel = Listen2MeRefined.Application.ViewModels.Windows.SettingsWindowViewModel;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class SettingsWindowViewModelThemeSettingsTests
{
    [Fact]
    public async Task InitializeAsync_LoadsPersistedThemeAndAccent()
    {
        var settings = new AppSettings
        {
            AutoCheckUpdatesOnStartup = false,
            ThemeMode = "Light",
            AccentColor = "Green"
        };

        var (viewModel, _, _) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        Assert.Equal("Light", viewModel.SelectedThemeMode);
        Assert.Equal("Green", viewModel.SelectedAccentColor);
    }

    [Fact]
    public async Task SelectedThemeModeChanged_PersistsAndAppliesTheme()
    {
        var settings = new AppSettings
        {
            AutoCheckUpdatesOnStartup = false,
            ThemeMode = "Dark",
            AccentColor = "Orange"
        };

        var (viewModel, appThemeService, _) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        viewModel.SelectedThemeMode = "Light";

        Assert.Equal("Light", settings.ThemeMode);
        appThemeService.Verify(x => x.ApplyTheme("Light", "Orange"), Times.Once);
    }

    [Fact]
    public async Task SelectedAccentColorChanged_PersistsAndAppliesTheme()
    {
        var settings = new AppSettings
        {
            AutoCheckUpdatesOnStartup = false,
            ThemeMode = "Dark",
            AccentColor = "Orange"
        };

        var (viewModel, appThemeService, _) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        viewModel.SelectedAccentColor = "Blue";

        Assert.Equal("Blue", settings.AccentColor);
        appThemeService.Verify(x => x.ApplyTheme("Dark", "Blue"), Times.Once);
    }

    private static (SettingsWindowViewModel ViewModel, Mock<IAppThemeService> AppThemeService, AppSettings Settings) CreateViewModel(AppSettings settings)
    {
        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);
        settingsManager
            .Setup(x => x.SaveSettings(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(apply => apply(settings));

        var settingsReadService = new AppSettingsReader(settingsManager.Object);
        var settingsWriteService = new AppSettingsWriter(settingsManager.Object);
        var playbackDefaultsService = new PlaybackDefaultsService(settingsManager.Object);

        var outputDevice = new Mock<IOutputDevice>();
        outputDevice.Setup(x => x.EnumerateOutputDevices()).Returns([]);

        var versionChecker = new Mock<IVersionChecker>();
        versionChecker.Setup(x => x.IsLatestAsync()).ReturnsAsync(true);

        var updateCheckService = new Mock<IAppUpdateChecker>();
        updateCheckService
            .Setup(x => x.CheckForUpdatesAsync())
            .ReturnsAsync(new AppUpdateCheckResult(
                false,
                "You are using the latest version.",
                false));

        var appThemeService = new Mock<IAppThemeService>();
        appThemeService.Setup(x => x.GetThemeModes()).Returns(["Dark", "Light"]);
        appThemeService.Setup(x => x.GetAccentColors()).Returns(["Orange", "Blue", "Green"]);

        var folderBrowser = new Mock<IFolderBrowser>();
        folderBrowser
            .Setup(x => x.DirectoryExists(It.IsAny<string>()))
            .Returns<string>(Directory.Exists);
        var pinnedFoldersService = new PinnedFoldersService(folderBrowser.Object);
        var playlistLibraryService = new Mock<IPlaylistLibraryService>();
        playlistLibraryService
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlaylistSummary>());
        
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        var viewModel = new SettingsWindowViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            Mock.Of<IMessenger>(),
            Mock.Of<IRepository<AudioModel>>(),
            Mock.Of<IMediator>(),
            new FontFamilies(["Segoe UI"]),
            Mock.Of<IRepository<MusicFolderModel>>(),
            Mock.Of<IRepository<PlaylistModel>>(),
            Mock.Of<IFolderScanner>(),
            Mock.Of<IFromFolderRemover>(),
            outputDevice.Object,
            versionChecker.Object,
            settingsReadService,
            settingsWriteService,
            updateCheckService.Object,
            Mock.Of<IBackgroundTaskStatusService>(),
            Mock.Of<IGlobalHookSettingsSyncService>(),
            pinnedFoldersService,
            playbackDefaultsService,
            playlistLibraryService.Object,
            appThemeService.Object);

        return (viewModel, appThemeService, settings);
    }
}
