using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Data.Repositories;
using Listen2MeRefined.Infrastructure.FolderBrowser;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Playlist;
using Listen2MeRefined.Infrastructure.Scanning.Folders;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.Infrastructure.Settings.Playback;
using Listen2MeRefined.Infrastructure.Utils;
using Listen2MeRefined.Infrastructure.Versioning;
using MediatR;
using Moq;
using Serilog;
using SettingsWindowViewModel = Listen2MeRefined.Infrastructure.ViewModels.SettingsWindowViewModel;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class SettingsWindowViewModelFolderBrowserSettingsTests
{
    [Fact]
    public async Task ClearInvalidPins_RemovesNonExistingEntries()
    {
        var validPath = Path.Combine(Path.GetTempPath(), "listen2me-test-pin");
        Directory.CreateDirectory(validPath);

        try
        {
            var settings = new AppSettings
            {
                AutoCheckUpdatesOnStartup = false,
                PinnedFolders = [validPath, @"Z:\DefinitelyMissing\Nope"]
            };
            var viewModel = CreateViewModel(settings);
            await viewModel.InitializeAsync();

            viewModel.ClearInvalidPinsCommand.Execute(null);

            Assert.Single(viewModel.PinnedFolders);
            Assert.Equal(validPath, viewModel.PinnedFolders[0]);
            Assert.Single(settings.PinnedFolders);
            Assert.Equal(validPath, settings.PinnedFolders[0]);
        }
        finally
        {
            if (Directory.Exists(validPath))
            {
                Directory.Delete(validPath);
            }
        }
    }

    [Fact]
    public async Task HandlePinnedFoldersChangedNotification_UpdatesPinnedFolders()
    {
        var settings = new AppSettings
        {
            AutoCheckUpdatesOnStartup = false,
            PinnedFolders = [@"C:\Before"]
        };

        var viewModel = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        await viewModel.Handle(
            new PinnedFoldersChangedNotification([@"D:\After"]),
            CancellationToken.None);

        Assert.Single(viewModel.PinnedFolders);
        Assert.Equal(@"D:\After", viewModel.PinnedFolders[0]);
    }

    [Fact]
    public async Task SelectedSearchResultsTransferModeChanged_PersistsSetting()
    {
        var settings = new AppSettings
        {
            AutoCheckUpdatesOnStartup = false,
            SearchResultsTransferMode = SearchResultsTransferMode.Move
        };

        var viewModel = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        viewModel.SelectedSearchResultsTransferMode = SearchResultsTransferMode.Copy;

        Assert.Equal(SearchResultsTransferMode.Copy, settings.SearchResultsTransferMode);
    }



    [Fact]
    public async Task ResetDroppedFolderPrompts_ClearsMutedPromptFolders()
    {
        var settings = new AppSettings
        {
            AutoCheckUpdatesOnStartup = false,
            MutedDroppedSongFolders = [@"C:\Music\A", @"D:\Music\B"]
        };

        var viewModel = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        viewModel.ResetDroppedFolderPromptsCommand.Execute(null);

        Assert.Empty(viewModel.MutedDroppedSongFolders);
        Assert.Empty(settings.MutedDroppedSongFolders);
    }

    [Fact]
    public async Task RefreshLibraryTabData_WhenSettingsChangedExternally_UpdatesFolders()
    {
        var settings = new AppSettings
        {
            AutoCheckUpdatesOnStartup = false,
            MusicFolders = [new MusicFolderModel(@"C:\Initial", false)]
        };

        var viewModel = CreateViewModel(settings);
        await viewModel.InitializeAsync();
        Assert.Single(viewModel.Folders);
        Assert.Equal(@"C:\Initial", viewModel.Folders[0]);

        settings.MusicFolders = [new MusicFolderModel(@"D:\External", true)];

        viewModel.RefreshLibraryTabData();

        Assert.Single(viewModel.Folders);
        Assert.Equal(@"D:\External", viewModel.Folders[0]);
        Assert.Equal(@"D:\External", viewModel.SelectedFolder);
        Assert.True(viewModel.SelectedFolderIncludeSubdirectories);
    }

    [Fact]
    public async Task RemoveFolder_WhenRecursionNeverEnabled_RemovesWithoutNullCrash()
    {
        var settings = new AppSettings
        {
            AutoCheckUpdatesOnStartup = false,
            MusicFolders = [new MusicFolderModel(@"C:\Music", false)]
        };

        var fromFolderRemover = new Mock<IFromFolderRemover>();
        fromFolderRemover
            .Setup(x => x.RemoveFromFolderAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var viewModel = CreateViewModel(settings, fromFolderRemover);
        await viewModel.InitializeAsync();
        viewModel.SelectedFolder = @"C:\Music";

        await viewModel.RemoveFolderCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Folders);
        Assert.Empty(settings.MusicFolders);
        fromFolderRemover.Verify(x => x.RemoveFromFolderAsync(@"C:\Music"), Times.Once);
    }

    private static SettingsWindowViewModel CreateViewModel(
        AppSettings settings,
        Mock<IFromFolderRemover>? fromFolderRemover = null)
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

        var folderBrowser = new Mock<IFolderBrowser>();
        folderBrowser
            .Setup(x => x.DirectoryExists(It.IsAny<string>()))
            .Returns<string>(Directory.Exists);
        var pinnedFoldersService = new PinnedFoldersService(folderBrowser.Object);
        var playlistLibraryService = new Mock<IPlaylistLibraryService>();
        playlistLibraryService
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlaylistSummary>());
        var appThemeService = new Mock<IAppThemeService>();
        appThemeService.Setup(x => x.GetThemeModes()).Returns(["Dark", "Light"]);
        appThemeService.Setup(x => x.GetAccentColors()).Returns(["Orange", "Blue", "Green"]);

        return new SettingsWindowViewModel(
            Mock.Of<ILogger>(),
            Mock.Of<IRepository<AudioModel>>(),
            Mock.Of<IMediator>(),
            new FontFamilies(["Segoe UI"]),
            Mock.Of<IRepository<MusicFolderModel>>(),
            Mock.Of<IRepository<PlaylistModel>>(),
            Mock.Of<IFolderScanner>(),
            (fromFolderRemover ?? new Mock<IFromFolderRemover>()).Object,
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
    }
}
