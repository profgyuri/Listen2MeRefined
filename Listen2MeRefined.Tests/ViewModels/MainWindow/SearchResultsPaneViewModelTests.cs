using System.Collections;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.ViewModels.ContextMenus;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Playlist;
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
        var queueState = CreateQueueState();
        var settingsReader = CreateSettingsReader(SearchResultsTransferMode.Move);
        var transferService = new Mock<ISearchResultsTransferService>();
        var contextMenu = CreateSongContextMenuViewModel(logger.Object, messenger);
        var vm = new SearchResultsPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            queueState,
            settingsReader.Object,
            Mock.Of<IAudioSearchExecutionService>(),
            transferService.Object,
            Mock.Of<IDefaultPlaylistService>(),
            Mock.Of<IPlaybackQueueActionsService>(),
            Mock.Of<Listen2MeRefined.Application.Playback.IMusicPlayerController>(),
            Mock.Of<Listen2MeRefined.Application.Files.IFileScanner>(),
            Mock.Of<Listen2MeRefined.Application.Utils.IObservableCollectionUpdater>(),
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
        var queueState = CreateQueueState();
        var contextMenu = CreateSongContextMenuViewModel(logger.Object, messenger);
        var vm = new SearchResultsPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            queueState,
            CreateSettingsReader(SearchResultsTransferMode.Move).Object,
            Mock.Of<IAudioSearchExecutionService>(),
            Mock.Of<ISearchResultsTransferService>(),
            Mock.Of<IDefaultPlaylistService>(),
            Mock.Of<IPlaybackQueueActionsService>(),
            Mock.Of<Listen2MeRefined.Application.Playback.IMusicPlayerController>(),
            Mock.Of<Listen2MeRefined.Application.Files.IFileScanner>(),
            Mock.Of<Listen2MeRefined.Application.Utils.IObservableCollectionUpdater>(),
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

    [Fact]
    public async Task AdvancedSearchRequestedMessage_ExecutesAndPublishesUpdatedResultsAndCompletion()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var queueState = CreateQueueState();
        var contextMenu = CreateSongContextMenuViewModel(logger.Object, messenger);
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        var vm = new SearchResultsPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            queueState,
            CreateSettingsReader(SearchResultsTransferMode.Move).Object,
            audioSearchExecutionService.Object,
            Mock.Of<ISearchResultsTransferService>(),
            Mock.Of<IDefaultPlaylistService>(),
            Mock.Of<IPlaybackQueueActionsService>(),
            Mock.Of<Listen2MeRefined.Application.Playback.IMusicPlayerController>(),
            Mock.Of<Listen2MeRefined.Application.Files.IFileScanner>(),
            Mock.Of<Listen2MeRefined.Application.Utils.IObservableCollectionUpdater>(),
            contextMenu);
        await vm.EnsureInitializedAsync();

        var expected = new AudioModel { Title = "Result", Path = "result.mp3" };
        SearchMatchMode? capturedMatchMode = null;
        audioSearchExecutionService
            .Setup(x => x.ExecuteAdvancedSearchAsync(It.IsAny<IEnumerable<AdvancedFilter>>(), It.IsAny<SearchMatchMode>()))
            .Callback<IEnumerable<AdvancedFilter>, SearchMatchMode>((_, mode) => capturedMatchMode = mode)
            .ReturnsAsync([expected]);

        SearchResultsUpdatedMessage? updated = null;
        AdvancedSearchCompletedMessage? completed = null;
        var recipient = new object();
        messenger.Register<object, SearchResultsUpdatedMessage>(recipient, (_, message) => updated = message);
        messenger.Register<object, AdvancedSearchCompletedMessage>(recipient, (_, message) => completed = message);

        messenger.Send(new AdvancedSearchRequestedMessage(new AdvancedSearchRequestedMessageData(
            [new AdvancedFilter(nameof(AudioModel.Title), AdvancedFilterOperator.Contains, "rock")],
            SearchMatchMode.Any)));

        for (var i = 0; i < 20 && (updated is null || completed is null); i++)
        {
            await Task.Delay(25);
        }

        Assert.Equal(SearchMatchMode.Any, capturedMatchMode);
        Assert.NotNull(updated);
        Assert.Single(updated!.Value);
        Assert.Same(expected, updated.Value[0]);
        Assert.NotNull(completed);
        Assert.Equal(1, completed!.Value);
    }

    private static PlaylistQueueState CreateQueueState() => new(new PlaylistQueue());

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
        settingsReader.Setup(x => x.GetFontFamily()).Returns("Segoe UI");
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
