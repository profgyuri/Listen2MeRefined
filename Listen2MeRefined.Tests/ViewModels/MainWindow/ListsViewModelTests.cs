using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using MediatR;
using Moq;
using Serilog;
using ListsViewModel = Listen2MeRefined.Application.ViewModels.Widgets.ListsViewModel;

namespace Listen2MeRefined.Tests.ViewModels.MainWindow;

public class ListsViewModelTests
{
    [Fact]
    public async Task SearchResultsToPlaylistRequestedMessage_AddsSongsToDefaultPlaylist()
    {
        var messenger = new WeakReferenceMessenger();
        var vm = CreateViewModel(out _, messenger);
        var first = new AudioModel { Title = "First", Path = "a" };
        var second = new AudioModel { Title = "Second", Path = "b" };
        await vm.EnsureInitializedAsync();
        messenger.Send(new SearchResultsToPlaylistRequestedMessage([first, second]));

        Assert.Equal(2, vm.PlayList.Count);
        Assert.Contains(first, vm.PlayList);
        Assert.Contains(second, vm.PlayList);
    }

    [Fact]
    public async Task SearchResultsToPlaylistRequestedMessage_DoesNotDuplicatePathEntriesInDefaultPlaylist()
    {
        var messenger = new WeakReferenceMessenger();
        var vm = CreateViewModel(out _, messenger);
        var first = new AudioModel { Title = "First", Path = "a" };
        var duplicateByPath = new AudioModel { Title = "First Duplicate", Path = "a" };
        await vm.EnsureInitializedAsync();
        messenger.Send(new SearchResultsToPlaylistRequestedMessage([first]));
        messenger.Send(new SearchResultsToPlaylistRequestedMessage([duplicateByPath]));

        Assert.Single(vm.PlayList);
        Assert.Contains(first, vm.PlayList);
        Assert.Single(vm.DefaultPlaylist);
        Assert.Equal("a", vm.DefaultPlaylist[0].Path);
    }

    [Fact]
    public void RemoveSelectedFromPlaylist_RemovesOnlySelectedItems()
    {
        var vm = CreateViewModel(out _);
        var first = new AudioModel { Title = "First", Path = "a" };
        var second = new AudioModel { Title = "Second", Path = "b" };

        vm.PlayList.Add(first);
        vm.PlayList.Add(second);
        vm.AddSelectedPlaylistItems([first]);

        vm.RemoveSelectedFromPlaylistCommand.Execute(null);

        Assert.Single(vm.PlayList);
        Assert.DoesNotContain(first, vm.PlayList);
        Assert.Contains(second, vm.PlayList);
    }

    [Fact]
    public async Task JumpToSelectedSongCommand_RequiresSelectedIndex()
    {
        var vm = CreateViewModel(out var playerController);

        Assert.False(vm.JumpToSelectedSongCommand.CanExecute(null));

        var first = new AudioModel { Title = "First", Path = "a" };
        var second = new AudioModel { Title = "Second", Path = "b" };
        vm.PlayList.Add(first);
        vm.PlayList.Add(second);
        vm.SelectedSong = second;
        vm.SelectedIndex = 1;
        Assert.True(vm.JumpToSelectedSongCommand.CanExecute(null));

        await vm.JumpToSelectedSongCommand.ExecuteAsync(null);
        playerController.Verify(x => x.JumpToIndexAsync(1), Times.Once);
    }

    [Fact]
    public async Task HandleAdvancedSearchNotification_UsesMatchModeAndPublishesCompletion()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        var messenger = new WeakReferenceMessenger();
        var mediator = new Mock<IMediator>();
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        var scanner = new Mock<IFileScanner>();
        var settingsReader = new Mock<IAppSettingsReader>();
        var playerController = new Mock<IMusicPlayerController>();
        var playlist = new PlaylistQueue();
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();
        settingsReader.Setup(x => x.GetMusicFolders()).Returns(Array.Empty<string>());
        settingsReader.Setup(x => x.GetMutedDroppedSongFolders()).Returns(Array.Empty<string>());
        var settingsWriter = new Mock<IAppSettingsWriter>();
        var prompt = new Mock<IDroppedSongFolderPromptService>();
        prompt.Setup(x => x.PromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddDroppedSongFolderDecision.Skip);
        settingsReader
            .Setup(x => x.GetSearchResultsTransferMode())
            .Returns(SearchResultsTransferMode.Move);
        var ui = new Mock<IUiDispatcher>();

        var vm = new ListsViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            mediator.Object,
            audioSearchExecutionService.Object,
            scanner.Object,
            settingsReader.Object,
            playerController.Object,
            playlist,
            settingsWriter.Object,
            prompt.Object,
            externalAudioOpenService.Object,
            ui.Object);

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
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        var mediator = new Mock<IMediator>();
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        var scanner = new Mock<IFileScanner>();
        var playerController = new Mock<IMusicPlayerController>();
        var playlist = new PlaylistQueue();
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetMusicFolders()).Returns(Array.Empty<string>());
        settingsReader.Setup(x => x.GetMutedDroppedSongFolders()).Returns(Array.Empty<string>());
        var settingsWriter = new Mock<IAppSettingsWriter>();
        var prompt = new Mock<IDroppedSongFolderPromptService>();
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();
        var ui = new Mock<IUiDispatcher>();

        var vm = new ListsViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            Mock.Of<IMessenger>(),
            mediator.Object,
            audioSearchExecutionService.Object,
            scanner.Object,
            settingsReader.Object,
            playerController.Object,
            playlist,
            settingsWriter.Object,
            prompt.Object,
            externalAudioOpenService.Object,
            ui.Object);

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
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        var mediator = new Mock<IMediator>();
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        var scanner = new Mock<IFileScanner>();
        var playerController = new Mock<IMusicPlayerController>();
        var playlist = new PlaylistQueue();
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetMusicFolders()).Returns(Array.Empty<string>());
        settingsReader.Setup(x => x.GetMutedDroppedSongFolders()).Returns(Array.Empty<string>());
        var settingsWriter = new Mock<IAppSettingsWriter>();
        var prompt = new Mock<IDroppedSongFolderPromptService>();
        prompt.Setup(x => x.PromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddDroppedSongFolderDecision.SkipAndDontAskAgain);
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();
        var uiDispatcher = new Mock<IUiDispatcher>();

        var vm = new ListsViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            Mock.Of<IMessenger>(),
            mediator.Object,
            audioSearchExecutionService.Object,
            scanner.Object,
            settingsReader.Object,
            playerController.Object,
            playlist,
            settingsWriter.Object,
            prompt.Object,
            externalAudioOpenService.Object,
            uiDispatcher.Object);

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

        settingsWriter.Verify(x => x.SetMutedDroppedSongFolders(It.Is<IEnumerable<string>>(f => f.Contains(Path.GetDirectoryName(filePath)!))), Times.Once);
    }

    [Fact]
    public async Task HandleExternalFileDropAsync_WhenNamedQueueIsActive_AddsOnlyToDefaultPlaylist()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        var mediator = new Mock<IMediator>();
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        var scanner = new Mock<IFileScanner>();
        var playerController = new Mock<IMusicPlayerController>();
        var playlist = new PlaylistQueue();
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetMusicFolders()).Returns(Array.Empty<string>());
        settingsReader.Setup(x => x.GetMutedDroppedSongFolders()).Returns(Array.Empty<string>());
        var settingsWriter = new Mock<IAppSettingsWriter>();
        var prompt = new Mock<IDroppedSongFolderPromptService>();
        prompt.Setup(x => x.PromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddDroppedSongFolderDecision.Skip);
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();
        var uiDispatcher = new Mock<IUiDispatcher>();

        var vm = new ListsViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            Mock.Of<IMessenger>(),
            mediator.Object,
            audioSearchExecutionService.Object,
            scanner.Object,
            settingsReader.Object,
            playerController.Object,
            playlist,
            settingsWriter.Object,
            prompt.Object,
            externalAudioOpenService.Object,
            uiDispatcher.Object);

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
    }

    private static ListsViewModel CreateViewModel(
        out Mock<IMusicPlayerController> playerController,
        IMessenger? messenger = null,
        SearchResultsTransferMode transferMode = SearchResultsTransferMode.Move)
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        var mediator = new Mock<IMediator>();
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        var scanner = new Mock<IFileScanner>();
        var settingsReader = new Mock<IAppSettingsReader>();
        playerController = new Mock<IMusicPlayerController>();
        var playlist = new PlaylistQueue();
        settingsReader.Setup(x => x.GetMusicFolders()).Returns(Array.Empty<string>());
        settingsReader.Setup(x => x.GetMutedDroppedSongFolders()).Returns(Array.Empty<string>());
        var settingsWriter = new Mock<IAppSettingsWriter>();
        var prompt = new Mock<IDroppedSongFolderPromptService>();
        prompt.Setup(x => x.PromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddDroppedSongFolderDecision.Skip);
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();
        settingsReader
            .Setup(x => x.GetSearchResultsTransferMode())
            .Returns(transferMode);
        var ui = new Mock<IUiDispatcher>();

        return new ListsViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger ?? Mock.Of<IMessenger>(),
            mediator.Object,
            audioSearchExecutionService.Object,
            scanner.Object,
            settingsReader.Object,
            playerController.Object,
            playlist,
            settingsWriter.Object,
            prompt.Object,
            externalAudioOpenService.Object,
            ui.Object);
    }
}

