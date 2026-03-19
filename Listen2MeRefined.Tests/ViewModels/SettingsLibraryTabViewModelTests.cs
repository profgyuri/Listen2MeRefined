using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Folders;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Application.ViewModels.SettingsTabs;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Core.Repositories;
using Listen2MeRefined.Infrastructure.FolderBrowser;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class SettingsLibraryTabViewModelTests
{
    [Fact]
    public async Task InitializeAsync_State_LoadsPersistedLibrarySettings()
    {
        var settings = new AppSettings
        {
            MusicFolders = [new MusicFolderModel(@"C:\Library", true)],
            AutoScanOnFolderAdd = false,
            ShowTaskPercentage = false,
            TaskPercentageReportInterval = 7,
            ShowScanMilestoneCount = true,
            ScanMilestoneInterval = 40,
            ScanMilestoneBasis = TaskStatusCountBasis.Remaining,
            FolderBrowserStartAtLastLocation = false,
            PinnedFolders = [@"C:\Pins"],
            MutedDroppedSongFolders = [@"C:\Muted"]
        };

        var (viewModel, _, _, _, _) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        Assert.Single(viewModel.Folders);
        Assert.Equal(@"C:\Library", viewModel.Folders[0]);
        Assert.Equal(@"C:\Library", viewModel.SelectedFolder);
        Assert.True(viewModel.SelectedFolderIncludeSubdirectories);
        Assert.False(viewModel.AutoScanOnFolderAdd);
        Assert.False(viewModel.ShowTaskPercentage);
        Assert.Equal(7, viewModel.TaskPercentageReportInterval);
        Assert.True(viewModel.ShowScanMilestoneCount);
        Assert.Equal(40, viewModel.ScanMilestoneInterval);
        Assert.Equal(TaskStatusCountBasis.Remaining, viewModel.SelectedScanMilestoneBasis);
        Assert.False(viewModel.FolderBrowserStartAtLastLocation);
        Assert.Single(viewModel.PinnedFolders);
        Assert.Single(viewModel.MutedDroppedSongFolders);
    }

    [Fact]
    public async Task RemoveFolderCommand_State_RemovesFolderAndPersists()
    {
        var settings = new AppSettings
        {
            MusicFolders = [new MusicFolderModel(@"C:\Music", false)]
        };
        var (viewModel, _, fromFolderRemover, _, _) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        viewModel.SelectedFolder = @"C:\Music";
        await viewModel.RemoveFolderCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Folders);
        Assert.Empty(settings.MusicFolders);
        fromFolderRemover.Verify(x => x.RemoveFromFolderAsync(@"C:\Music"), Times.Once);
    }

    [Fact]
    public async Task ClearInvalidPinsCommand_State_RemovesInvalidPins()
    {
        var validPath = Path.Combine(Path.GetTempPath(), "listen2me-stage4-pin");
        Directory.CreateDirectory(validPath);
        try
        {
            var settings = new AppSettings
            {
                PinnedFolders = [validPath, @"Z:\Invalid\Missing"]
            };

            var (viewModel, _, _, _, _) = CreateViewModel(settings);
            await viewModel.InitializeAsync();
            await viewModel.ClearInvalidPinsCommand.ExecuteAsync(null);

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
    public async Task ShowTaskPercentageChanged_State_PersistsAndRefreshesSnapshot()
    {
        var settings = new AppSettings
        {
            ShowTaskPercentage = true
        };

        var (viewModel, _, _, _, taskStatusService) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        viewModel.ShowTaskPercentage = false;

        Assert.False(settings.ShowTaskPercentage);
        taskStatusService.Verify(x => x.RefreshSnapshot(), Times.Once);
    }

    [Fact]
    public async Task ForceScanCommand_WhenScannerThrows_UsesErrorHandler()
    {
        var settings = new AppSettings();
        var (viewModel, errorHandler, _, folderScanner, _) = CreateViewModel(settings, throwOnScanAll: true);
        await viewModel.InitializeAsync();

        await viewModel.ForceScanCommand.ExecuteAsync(null);

        folderScanner.Verify(x => x.ScanAllAsync(ScanMode.FullRefresh, It.IsAny<CancellationToken>()), Times.Once);
        errorHandler.Verify(
            x => x.HandleAsync(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static (
        SettingsLibraryTabViewModel ViewModel,
        Mock<IErrorHandler> ErrorHandler,
        Mock<IFromFolderRemover> FromFolderRemover,
        Mock<IFolderScanner> FolderScanner,
        Mock<IBackgroundTaskStatusService> TaskStatusService) CreateViewModel(
            AppSettings settings,
            bool throwOnScanAll = false)
    {
        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);
        settingsManager
            .Setup(x => x.SaveSettings(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(apply => apply(settings));

        var folderBrowser = new Mock<IFolderBrowser>();
        folderBrowser
            .Setup(x => x.DirectoryExists(It.IsAny<string>()))
            .Returns<string>(Directory.Exists);
        var pinnedFoldersService = new PinnedFoldersService(folderBrowser.Object);

        var fromFolderRemover = new Mock<IFromFolderRemover>();
        fromFolderRemover
            .Setup(x => x.RemoveFromFolderAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var folderScanner = new Mock<IFolderScanner>();
        if (throwOnScanAll)
        {
            folderScanner
                .Setup(x => x.ScanAllAsync(It.IsAny<ScanMode>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Scan failure"));
        }
        else
        {
            folderScanner
                .Setup(x => x.ScanAllAsync(It.IsAny<ScanMode>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        var taskStatusService = new Mock<IBackgroundTaskStatusService>();
        taskStatusService
            .Setup(x => x.GetSnapshot())
            .Returns(new BackgroundTaskSnapshot(false, null, 0));

        var errorHandler = new Mock<IErrorHandler>();
        errorHandler
            .Setup(x => x.HandleAsync(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        var viewModel = new SettingsLibraryTabViewModel(
            errorHandler.Object,
            logger.Object,
            new WeakReferenceMessenger(),
            new AppSettingsReader(settingsManager.Object),
            new AppSettingsWriter(settingsManager.Object),
            fromFolderRemover.Object,
            folderScanner.Object,
            pinnedFoldersService,
            taskStatusService.Object);

        return (viewModel, errorHandler, fromFolderRemover, folderScanner, taskStatusService);
    }
}
