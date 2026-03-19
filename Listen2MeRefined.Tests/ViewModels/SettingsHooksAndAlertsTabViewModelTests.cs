using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.ViewModels.SettingsTabs;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class SettingsHooksAndAlertsTabViewModelTests
{
    [Fact]
    public async Task InitializeAsync_State_LoadsPersistedHookSettings()
    {
        var settings = new AppSettings
        {
            FontFamily = "Consolas",
            NewSongWindowPosition = "Always on top",
            EnableGlobalMediaKeys = false,
            EnableCornerNowPlayingPopup = true,
            CornerTriggerSizePx = 22,
            CornerTriggerDebounceMs = 45
        };

        var (viewModel, _, _, _, _) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        Assert.Equal("Consolas", viewModel.FontFamilyName);
        Assert.Equal("Always on top", viewModel.SelectedNewSongWindowPosition);
        Assert.False(viewModel.EnableGlobalMediaKeys);
        Assert.True(viewModel.EnableCornerNowPlayingPopup);
        Assert.Equal(22, viewModel.CornerTriggerSizePx);
        Assert.Equal(45, viewModel.CornerTriggerDebounceMs);
        Assert.Equal(2, viewModel.NewSongWindowPositions.Count);
        Assert.Equal("Default", viewModel.NewSongWindowPositions[0]);
        Assert.Equal("Always on top", viewModel.NewSongWindowPositions[1]);
    }

    [Fact]
    public async Task SelectedNewSongWindowPosition_State_PersistsAndPublishesMessage()
    {
        var settings = new AppSettings
        {
            NewSongWindowPosition = "Default"
        };

        var (viewModel, _, _, probe, _) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        viewModel.SelectedNewSongWindowPosition = "Always on top";

        Assert.Equal("Always on top", settings.NewSongWindowPosition);
        Assert.Equal("Always on top", probe.Position);
    }

    [Fact]
    public async Task EnableGlobalMediaKeys_State_PersistsAndSyncsGlobalHookRegistration()
    {
        var settings = new AppSettings
        {
            EnableGlobalMediaKeys = false,
            EnableCornerNowPlayingPopup = true
        };

        var syncSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var (viewModel, syncService, _, _, _) = CreateViewModel(settings, syncSignal: syncSignal);
        await viewModel.InitializeAsync();

        viewModel.EnableGlobalMediaKeys = true;
        await syncSignal.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(settings.EnableGlobalMediaKeys);
        syncService.Verify(x => x.SyncAsync(true, true), Times.Once);
    }

    [Fact]
    public async Task CornerTriggerDebounceMs_State_ClampsAndPersists()
    {
        var settings = new AppSettings
        {
            CornerTriggerDebounceMs = 10
        };

        var (viewModel, _, _, _, _) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        viewModel.CornerTriggerDebounceMs = 250;

        Assert.Equal(200, viewModel.CornerTriggerDebounceMs);
        Assert.Equal((short)200, settings.CornerTriggerDebounceMs);
    }

    [Fact]
    public async Task SyncGlobalHookRegistration_WhenSyncThrows_UsesErrorHandler()
    {
        var settings = new AppSettings
        {
            EnableGlobalMediaKeys = true,
            EnableCornerNowPlayingPopup = false
        };

        var errorSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var (viewModel, _, errorHandler, _, _) = CreateViewModel(
            settings,
            throwOnSync: true,
            errorSignal: errorSignal);
        await viewModel.InitializeAsync();

        viewModel.EnableCornerNowPlayingPopup = true;
        await errorSignal.Task.WaitAsync(TimeSpan.FromSeconds(2));

        errorHandler.Verify(
            x => x.HandleAsync(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FontFamilyChangedMessage_State_UpdatesFontFamilyName()
    {
        var settings = new AppSettings
        {
            FontFamily = "Segoe UI"
        };

        var (viewModel, _, _, _, messenger) = CreateViewModel(settings);
        await viewModel.InitializeAsync();

        messenger.Send(new FontFamilyChangedMessage("Courier New"));

        Assert.Equal("Courier New", viewModel.FontFamilyName);
    }

    private static (
        SettingsHooksAndAlertsTabViewModel ViewModel,
        Mock<IGlobalHookSettingsSyncService> SyncService,
        Mock<IErrorHandler> ErrorHandler,
        MessageProbe Probe,
        WeakReferenceMessenger Messenger) CreateViewModel(
            AppSettings settings,
            bool throwOnSync = false,
            TaskCompletionSource? errorSignal = null,
            TaskCompletionSource? syncSignal = null)
    {
        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);
        settingsManager
            .Setup(x => x.SaveSettings(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(apply => apply(settings));

        var syncService = new Mock<IGlobalHookSettingsSyncService>();
        if (throwOnSync)
        {
            syncService
                .Setup(x => x.SyncAsync(It.IsAny<bool>(), It.IsAny<bool>()))
                .ThrowsAsync(new InvalidOperationException("Hook sync failure"));
        }
        else
        {
            syncService
                .Setup(x => x.SyncAsync(It.IsAny<bool>(), It.IsAny<bool>()))
                .Callback(() => syncSignal?.TrySetResult())
                .Returns(Task.CompletedTask);
        }

        var errorHandler = new Mock<IErrorHandler>();
        errorHandler
            .Setup(x => x.HandleAsync(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => errorSignal?.TrySetResult())
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        var messenger = new WeakReferenceMessenger();
        var probe = new MessageProbe();
        messenger.Register<MessageProbe, CornerWindowPositionChangedMessage>(
            probe,
            static (recipient, message) => recipient.Position = message.Value);

        var viewModel = new SettingsHooksAndAlertsTabViewModel(
            errorHandler.Object,
            logger.Object,
            messenger,
            new AppSettingsReader(settingsManager.Object),
            new AppSettingsWriter(settingsManager.Object),
            syncService.Object);

        return (viewModel, syncService, errorHandler, probe, messenger);
    }

    private sealed class MessageProbe
    {
        public string? Position { get; set; }
    }
}
