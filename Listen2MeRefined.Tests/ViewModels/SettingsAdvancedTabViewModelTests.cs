using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.ViewModels.SettingsTabs;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Core.Repositories;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class SettingsAdvancedTabViewModelTests
{
    [Fact]
    public async Task InitializeAsync_State_LoadsFontAndResetUiState()
    {
        var settings = new AppSettings
        {
            FontFamily = "Consolas"
        };

        var (viewModel, _, _, _, _, _) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        Assert.Equal("Consolas", viewModel.FontFamilyName);
        Assert.True(viewModel.IsClearMetadataButtonVisible);
        Assert.False(viewModel.IsCancelClearMetadataButtonVisible);
        Assert.Equal("Cancel (5)", viewModel.CancelClearMetadataButtonContent);
    }

    [Fact]
    public async Task ClearMetadataCommand_State_ClearsRepositoriesAndSettings()
    {
        var settings = new AppSettings
        {
            MusicFolders = [new MusicFolderModel(@"C:\Music", false)]
        };

        var (viewModel, audioRepository, musicFolderRepository, playlistRepository, _, _) = CreateViewModel(
            settings,
            countdownStartSeconds: 1,
            countdownTickInterval: TimeSpan.FromMilliseconds(5));
        await viewModel.InitializeAsync();

        await viewModel.ClearMetadataCommand.ExecuteAsync(null);

        audioRepository.Verify(x => x.RemoveAllAsync(), Times.Once);
        musicFolderRepository.Verify(x => x.RemoveAllAsync(), Times.Once);
        playlistRepository.Verify(x => x.RemoveAllAsync(), Times.Once);
        Assert.Empty(settings.MusicFolders);
        Assert.True(viewModel.IsClearMetadataButtonVisible);
        Assert.False(viewModel.IsCancelClearMetadataButtonVisible);
        Assert.Equal("Cancel (1)", viewModel.CancelClearMetadataButtonContent);
    }

    [Fact]
    public async Task CancelClearMetadataCommand_State_CancelsCountdownWithoutClearing()
    {
        var settings = new AppSettings();
        var (viewModel, audioRepository, musicFolderRepository, playlistRepository, _, _) = CreateViewModel(
            settings,
            countdownStartSeconds: 5,
            countdownTickInterval: TimeSpan.FromMilliseconds(80));
        await viewModel.InitializeAsync();

        var clearTask = viewModel.ClearMetadataCommand.ExecuteAsync(null);
        Assert.True(viewModel.IsCancelClearMetadataButtonVisible);

        await viewModel.CancelClearMetadataCommand.ExecuteAsync(null);
        await clearTask;

        audioRepository.Verify(x => x.RemoveAllAsync(), Times.Never);
        musicFolderRepository.Verify(x => x.RemoveAllAsync(), Times.Never);
        playlistRepository.Verify(x => x.RemoveAllAsync(), Times.Never);
        Assert.True(viewModel.IsClearMetadataButtonVisible);
        Assert.False(viewModel.IsCancelClearMetadataButtonVisible);
    }

    [Fact]
    public async Task ClearMetadataCommand_WhenRepositoryThrows_UsesErrorHandler()
    {
        var settings = new AppSettings();
        var (viewModel, _, _, _, errorHandler, _) = CreateViewModel(
            settings,
            throwOnAudioRemoveAll: true,
            countdownStartSeconds: 0,
            countdownTickInterval: TimeSpan.FromMilliseconds(1));
        await viewModel.InitializeAsync();

        await viewModel.ClearMetadataCommand.ExecuteAsync(null);

        errorHandler.Verify(
            x => x.HandleAsync(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.True(viewModel.IsClearMetadataButtonVisible);
        Assert.False(viewModel.IsCancelClearMetadataButtonVisible);
    }

    [Fact]
    public async Task Dispose_DuringCountdown_CancelsWithoutClearing()
    {
        var settings = new AppSettings();
        var (viewModel, audioRepository, musicFolderRepository, playlistRepository, _, _) = CreateViewModel(
            settings,
            countdownStartSeconds: 5,
            countdownTickInterval: TimeSpan.FromMilliseconds(80));
        await viewModel.InitializeAsync();

        var clearTask = viewModel.ClearMetadataCommand.ExecuteAsync(null);

        viewModel.Dispose();
        await clearTask;

        audioRepository.Verify(x => x.RemoveAllAsync(), Times.Never);
        musicFolderRepository.Verify(x => x.RemoveAllAsync(), Times.Never);
        playlistRepository.Verify(x => x.RemoveAllAsync(), Times.Never);
    }

    [Fact]
    public async Task FontFamilyChangedMessage_State_UpdatesFontFamilyName()
    {
        var settings = new AppSettings
        {
            FontFamily = "Segoe UI"
        };

        var (viewModel, _, _, _, _, messenger) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        messenger.Send(new FontFamilyChangedMessage("Courier New"));

        Assert.Equal("Courier New", viewModel.FontFamilyName);
    }

    private static (
        SettingsAdvancedTabViewModel ViewModel,
        Mock<IRepository<AudioModel>> AudioRepository,
        Mock<IRepository<MusicFolderModel>> MusicFolderRepository,
        Mock<IRepository<PlaylistModel>> PlaylistRepository,
        Mock<IErrorHandler> ErrorHandler,
        WeakReferenceMessenger Messenger) CreateViewModel(
            AppSettings settings,
            bool throwOnAudioRemoveAll = false,
            int countdownStartSeconds = 5,
            TimeSpan? countdownTickInterval = null)
    {
        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);
        settingsManager
            .Setup(x => x.SaveSettings(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(apply => apply(settings));

        var audioRepository = new Mock<IRepository<AudioModel>>();
        if (throwOnAudioRemoveAll)
        {
            audioRepository
                .Setup(x => x.RemoveAllAsync())
                .ThrowsAsync(new InvalidOperationException("Audio remove failed"));
        }
        else
        {
            audioRepository
                .Setup(x => x.RemoveAllAsync())
                .Returns(Task.CompletedTask);
        }

        var musicFolderRepository = new Mock<IRepository<MusicFolderModel>>();
        musicFolderRepository
            .Setup(x => x.RemoveAllAsync())
            .Returns(Task.CompletedTask);

        var playlistRepository = new Mock<IRepository<PlaylistModel>>();
        playlistRepository
            .Setup(x => x.RemoveAllAsync())
            .Returns(Task.CompletedTask);

        var errorHandler = new Mock<IErrorHandler>();
        errorHandler
            .Setup(x => x.HandleAsync(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        var messenger = new WeakReferenceMessenger();
        var viewModel = new SettingsAdvancedTabViewModel(
            errorHandler.Object,
            logger.Object,
            messenger,
            audioRepository.Object,
            musicFolderRepository.Object,
            playlistRepository.Object,
            new AppSettingsReader(settingsManager.Object),
            new AppSettingsWriter(settingsManager.Object),
            countdownStartSeconds,
            countdownTickInterval);

        return (viewModel, audioRepository, musicFolderRepository, playlistRepository, errorHandler, messenger);
    }
}
