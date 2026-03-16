using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Playlist;
using MediatR;
using Moq;
using Serilog;
using ListsViewModel = Listen2MeRefined.Application.ViewModels.Widgets.ListsViewModel;

namespace Listen2MeRefined.Tests.ViewModels.MainWindow;

public class ListsViewModelTests
{
    [Fact]
    public async Task HandleAdvancedSearchNotification_UsesMatchModeAndPublishesCompletion()
    {
        var logger = CreateLogger();
        var mediator = new Mock<IMediator>();
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        var queueServices = CreateQueueServices(logger.Object);
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();
        var messenger = new WeakReferenceMessenger();

        var vm = new ListsViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            mediator.Object,
            audioSearchExecutionService.Object,
            queueServices.State,
            queueServices.RoutingService,
            queueServices.DropImportService,
            queueServices.PlaybackContextSyncService,
            externalAudioOpenService.Object);

        SearchResultsUpdatedMessage? publishedMessage = null;
        var recipient = new object();
        messenger.Register<object, SearchResultsUpdatedMessage>(recipient, (_, message) => publishedMessage = message);

        SearchMatchMode? capturedMatchMode = null;
        audioSearchExecutionService
            .Setup(x => x.ExecuteAdvancedSearchAsync(It.IsAny<IEnumerable<AdvancedFilter>>(), It.IsAny<SearchMatchMode>()))
            .Callback<IEnumerable<AdvancedFilter>, SearchMatchMode>((_, mode) => capturedMatchMode = mode)
            .ReturnsAsync([]);
        mediator
            .Setup(x => x.Publish(It.IsAny<AdvancedSearchCompletedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await vm.Handle(
            new AdvancedSearchNotification(
                [new AdvancedFilter(nameof(AudioModel.Title), AdvancedFilterOperator.Contains, "rock")],
                SearchMatchMode.Any),
            CancellationToken.None);

        Assert.Equal(SearchMatchMode.Any, capturedMatchMode);
        Assert.NotNull(publishedMessage);
        Assert.Empty(publishedMessage!.Value);
        mediator.Verify(
            x => x.Publish(It.Is<AdvancedSearchCompletedNotification>(n => n.ResultCount == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleExternalFileDropAsync_InsertsScannedSongsAtDropIndex()
    {
        var vm = CreateViewModel(out var scanner, out _);
        var existing = new AudioModel { Path = "existing.mp3", Title = "Existing" };
        vm.PlayList.Add(existing);

        var filePath = Path.Combine(Path.GetTempPath(), $"drop-{Guid.NewGuid():N}.mp3");
        await File.WriteAllTextAsync(filePath, "fake");

        var scanned = new AudioModel { Path = filePath, Title = "Dropped" };
        scanner.Setup(x => x.ScanAsync(filePath, It.IsAny<CancellationToken>())).ReturnsAsync(scanned);

        try
        {
            await vm.HandleExternalFileDropAsync([filePath], 0);
        }
        finally
        {
            File.Delete(filePath);
        }

        Assert.Equal(scanned, vm.PlayList[0]);
        Assert.Equal(existing, vm.PlayList[1]);
        Assert.Single(vm.DefaultPlaylist);
        Assert.Equal(scanned, vm.DefaultPlaylist[0]);
    }

    [Fact]
    public async Task HandleExternalFileDropAsync_WhenUserChoosesDontAskAgain_PersistsPreference()
    {
        var logger = CreateLogger();
        var mediator = new Mock<IMediator>();
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        var scanner = new Mock<IFileScanner>();
        var settingsReader = CreateSettingsReader();
        var settingsWriter = new Mock<IAppSettingsWriter>();
        var prompt = new Mock<IDroppedSongFolderPromptService>();
        prompt.Setup(x => x.PromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddDroppedSongFolderDecision.SkipAndDontAskAgain);
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();
        var queueServices = CreateQueueServices(
            logger.Object,
            scanner: scanner,
            settingsReader: settingsReader,
            settingsWriter: settingsWriter,
            promptService: prompt);

        var vm = new ListsViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            new WeakReferenceMessenger(),
            mediator.Object,
            audioSearchExecutionService.Object,
            queueServices.State,
            queueServices.RoutingService,
            queueServices.DropImportService,
            queueServices.PlaybackContextSyncService,
            externalAudioOpenService.Object);

        var filePath = Path.Combine(Path.GetTempPath(), $"drop-{Guid.NewGuid():N}.mp3");
        await File.WriteAllTextAsync(filePath, "fake");

        scanner.Setup(x => x.ScanAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioModel { Path = filePath, Title = "Dropped" });

        try
        {
            await vm.HandleExternalFileDropAsync([filePath], 0);
        }
        finally
        {
            File.Delete(filePath);
        }

        settingsWriter.Verify(x =>
            x.SetMutedDroppedSongFolders(It.Is<IEnumerable<string>>(f => f.Contains(Path.GetDirectoryName(filePath)!))), Times.Once);
    }

    [Fact]
    public async Task HandleExternalFileDropAsync_WhenNamedQueueIsActive_AddsOnlyToDefaultPlaylist()
    {
        var vm = CreateViewModel(out var scanner, out var queueServices);
        var activeQueueSong = new AudioModel { Path = "named.mp3", Title = "Named Queue Song" };
        vm.ActivateNamedPlaylistQueue(99, [activeQueueSong]);

        var filePath = Path.Combine(Path.GetTempPath(), $"drop-{Guid.NewGuid():N}.mp3");
        await File.WriteAllTextAsync(filePath, "fake");

        var scanned = new AudioModel { Path = filePath, Title = "Dropped" };
        scanner.Setup(x => x.ScanAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanned);

        try
        {
            await vm.HandleExternalFileDropAsync([filePath], 0);
        }
        finally
        {
            File.Delete(filePath);
        }

        Assert.Single(vm.DefaultPlaylist);
        Assert.Equal(scanned, vm.DefaultPlaylist[0]);
        Assert.Single(vm.PlayList);
        Assert.Equal(activeQueueSong, vm.PlayList[0]);
        Assert.False(queueServices.State.IsDefaultPlaylistActive);
    }

    private static ListsViewModel CreateViewModel(
        out Mock<IFileScanner> scanner,
        out QueueServices queueServices)
    {
        var logger = CreateLogger();
        var mediator = new Mock<IMediator>();
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        scanner = new Mock<IFileScanner>();
        queueServices = CreateQueueServices(logger.Object, scanner: scanner);
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();

        return new ListsViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            new WeakReferenceMessenger(),
            mediator.Object,
            audioSearchExecutionService.Object,
            queueServices.State,
            queueServices.RoutingService,
            queueServices.DropImportService,
            queueServices.PlaybackContextSyncService,
            externalAudioOpenService.Object);
    }

    private static QueueServices CreateQueueServices(
        ILogger logger,
        Mock<IFileScanner>? scanner = null,
        Mock<IAppSettingsReader>? settingsReader = null,
        Mock<IAppSettingsWriter>? settingsWriter = null,
        Mock<IDroppedSongFolderPromptService>? promptService = null)
    {
        var fileScanner = scanner ?? new Mock<IFileScanner>();
        var reader = settingsReader ?? CreateSettingsReader();
        var writer = settingsWriter ?? new Mock<IAppSettingsWriter>();
        var prompt = promptService ?? CreatePrompt();
        var musicPlayerController = new Mock<IMusicPlayerController>();
        var playlistQueue = new PlaylistQueue();
        var queueState = new PlaylistQueueState(playlistQueue);
        var playbackContextSync = new PlaybackContextSyncService(queueState);

        return new QueueServices(
            queueState,
            new DefaultPlaylistService(queueState),
            new PlaylistQueueRoutingService(queueState, playlistQueue, musicPlayerController.Object),
            playbackContextSync,
            new ExternalDropImportService(
                queueState,
                fileScanner.Object,
                reader.Object,
                writer.Object,
                prompt.Object));
    }

    private static Mock<IAppSettingsReader> CreateSettingsReader()
    {
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetMusicFolders()).Returns(Array.Empty<string>());
        settingsReader.Setup(x => x.GetMutedDroppedSongFolders()).Returns(Array.Empty<string>());
        return settingsReader;
    }

    private static Mock<IDroppedSongFolderPromptService> CreatePrompt()
    {
        var prompt = new Mock<IDroppedSongFolderPromptService>();
        prompt.Setup(x => x.PromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddDroppedSongFolderDecision.Skip);
        return prompt;
    }

    private static Mock<ILogger> CreateLogger()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        return logger;
    }

    private sealed record QueueServices(
        PlaylistQueueState State,
        IDefaultPlaylistService DefaultPlaylistService,
        IPlaylistQueueRoutingService RoutingService,
        IPlaybackContextSyncService PlaybackContextSyncService,
        IExternalDropImportService DropImportService);
}
