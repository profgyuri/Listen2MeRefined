using System.Collections;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.ContextMenus;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using MediatR;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels.MainWindow;

public class SearchResultsPaneViewModelTests
{
    [Fact]
    public async Task SendSelectedToPlaylist_UsesTransferDecisionAndPublishesPlaylistRequest()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var lists = CreateListsViewModel(logger.Object, messenger);
        var settingsReader = CreateSettingsReader(SearchResultsTransferMode.Move);
        var transferService = new Mock<ISearchResultsTransferService>();
        var contextMenu = CreateSongContextMenuViewModel(logger.Object, messenger);
        var vm = new SearchResultsPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            lists,
            settingsReader.Object,
            transferService.Object,
            contextMenu);

        var first = new AudioModel { Title = "First", Path = "a.mp3" };
        var second = new AudioModel { Title = "Second", Path = "b.mp3" };
        transferService
            .Setup(x => x.ResolveTransfer(
                It.IsAny<IEnumerable<AudioModel>>(),
                It.IsAny<IEnumerable<AudioModel>>(),
                SearchResultsTransferMode.Move))
            .Returns(new SearchResultsTransferDecision([first], [first], ClearSelection: true));

        SearchResultsToPlaylistRequestedMessage? publishedMessage = null;
        var recipient = new object();
        messenger.Register<object, SearchResultsToPlaylistRequestedMessage>(
            recipient,
            (_, message) => publishedMessage = message);

        await vm.EnsureInitializedAsync();
        vm.SearchResults.Add(first);
        vm.SearchResults.Add(second);
        await vm.SearchResultsSelectionAddedCommand.ExecuteAsync(new ArrayList { first });

        await vm.SendSelectedToPlaylistCommand.ExecuteAsync(null);

        Assert.NotNull(publishedMessage);
        Assert.Single(publishedMessage!.Value);
        Assert.Same(first, publishedMessage.Value[0]);
        Assert.Single(vm.SearchResults);
        Assert.Contains(second, vm.SearchResults);
        Assert.Empty(vm.GetDirectSongContextSelection());
    }

    [Fact]
    public async Task SearchResultsUpdatedMessage_ReplacesVisibleResultsAndClearsSelection()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var lists = CreateListsViewModel(logger.Object, messenger);
        var contextMenu = CreateSongContextMenuViewModel(logger.Object, messenger);
        var vm = new SearchResultsPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            lists,
            CreateSettingsReader(SearchResultsTransferMode.Move).Object,
            Mock.Of<ISearchResultsTransferService>(),
            contextMenu);

        var original = new AudioModel { Title = "Original", Path = "original.mp3" };
        var replacement = new AudioModel { Title = "Replacement", Path = "replacement.mp3" };
        await vm.EnsureInitializedAsync();
        vm.SearchResults.Add(original);
        await vm.SearchResultsSelectionAddedCommand.ExecuteAsync(new ArrayList { original });

        messenger.Send(new SearchResultsUpdatedMessage([replacement]));

        Assert.Single(vm.SearchResults);
        Assert.Same(replacement, vm.SearchResults[0]);
        Assert.Empty(vm.GetDirectSongContextSelection());
    }

    private static ListsViewModel CreateListsViewModel(ILogger logger, IMessenger messenger)
    {
        var mediator = new Mock<IMediator>();
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        var scanner = new Mock<IFileScanner>();
        var settingsReader = CreateSettingsReader(SearchResultsTransferMode.Move);
        var playerController = new Mock<IMusicPlayerController>();
        var playlist = new PlaylistQueue();
        var settingsWriter = new Mock<IAppSettingsWriter>();
        var prompt = new Mock<IDroppedSongFolderPromptService>();
        prompt.Setup(x => x.PromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddDroppedSongFolderDecision.Skip);
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();
        var ui = new Mock<IUiDispatcher>();

        return new ListsViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
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
    }

    private static SongContextMenuViewModel CreateSongContextMenuViewModel(ILogger logger, IMessenger messenger)
    {
        return new SongContextMenuViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            Mock.Of<IPlaylistMembership>(),
            Mock.Of<ISongContextSelectionService>());
    }

    private static Mock<IAppSettingsReader> CreateSettingsReader(SearchResultsTransferMode mode)
    {
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetMusicFolders()).Returns(Array.Empty<string>());
        settingsReader.Setup(x => x.GetMutedDroppedSongFolders()).Returns(Array.Empty<string>());
        settingsReader.Setup(x => x.GetSearchResultsTransferMode()).Returns(mode);
        return settingsReader;
    }

    private static Mock<ILogger> CreateLogger()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        return logger;
    }
}
