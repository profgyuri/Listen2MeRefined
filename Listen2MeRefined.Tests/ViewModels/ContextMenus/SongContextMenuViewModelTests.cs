using System.Collections;
using System.Collections.ObjectModel;
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
using Listen2MeRefined.Infrastructure.Playlist;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels.ContextMenus;

public class SongContextMenuViewModelTests
{
    [Fact]
    public async Task HandleOpenedAsync_SearchHostSelection_PopulatesPlaylists()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var contextMenuService = new Mock<IPlaylistMembership>();
        var selectionService = new Mock<ISongContextSelectionService>();
        selectionService
            .Setup(x => x.ResolveSelectionPaths(It.IsAny<IEnumerable<AudioModel>>(), It.IsAny<IEnumerable<AudioModel>>(), It.IsAny<AudioModel?>()))
            .Returns(["a.mp3"]);
        contextMenuService
            .Setup(x => x.GetPlaylistMembershipInfoAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlaylistMembershipInfo(12, "Workout", true)]);

        var songContextMenuVm = new SongContextMenuViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            contextMenuService.Object,
            selectionService.Object);

        var queueState = CreateQueueState();
        var searchVm = new SearchResultsPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            queueState,
            CreateSettingsReader(),
            CreateAudioSearchService(),
            Mock.Of<ISearchResultsTransferService>(),
            Mock.Of<IDefaultPlaylistService>(),
            Mock.Of<IPlaybackQueueActionsService>(),
            Mock.Of<Listen2MeRefined.Application.Playback.IMusicPlayerController>(),
            Mock.Of<Listen2MeRefined.Application.Files.IFileScanner>(),
            Mock.Of<IObservableCollectionUpdater>(),
            songContextMenuVm);

        searchVm.SearchResultsSelectionAddedCommand.Execute(new ArrayList
        {
            new AudioModel { Path = "a.mp3", Title = "Track A" }
        });

        await searchVm.InitializeAsync();
        await songContextMenuVm.HandleOpenedAsync();

        Assert.Single(songContextMenuVm.Playlists);
        Assert.Equal("Workout", songContextMenuVm.Playlists[0].PlaylistName);
        Assert.True(songContextMenuVm.ShowPlaylistActions);
        Assert.False(songContextMenuVm.ShowRemoveFromPlaylistAction);
        Assert.True(songContextMenuVm.ShowAddToDefaultPlaylistAction);
        contextMenuService.Verify(
            x => x.GetPlaylistMembershipInfoAsync(
                It.Is<IReadOnlyList<string>>(paths => paths.SequenceEqual(new[] { "a.mp3" })),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddToNewPlaylistAsync_ValidName_DelegatesToContextMenuService()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var contextMenuService = new Mock<IPlaylistMembership>();
        var selectionService = new Mock<ISongContextSelectionService>();
        selectionService
            .Setup(x => x.ResolveSelectionPaths(It.IsAny<IEnumerable<AudioModel>>(), It.IsAny<IEnumerable<AudioModel>>(), It.IsAny<AudioModel?>()))
            .Returns(["a.mp3", "b.mp3"]);
        contextMenuService
            .Setup(x => x.GetPlaylistMembershipInfoAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlaylistMembershipInfo>());

        var songContextMenuVm = new SongContextMenuViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            contextMenuService.Object,
            selectionService.Object);

        var queueState = CreateQueueState();
        var searchVm = new SearchResultsPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            queueState,
            CreateSettingsReader(),
            CreateAudioSearchService(),
            Mock.Of<ISearchResultsTransferService>(),
            Mock.Of<IDefaultPlaylistService>(),
            Mock.Of<IPlaybackQueueActionsService>(),
            Mock.Of<Listen2MeRefined.Application.Playback.IMusicPlayerController>(),
            Mock.Of<Listen2MeRefined.Application.Files.IFileScanner>(),
            Mock.Of<IObservableCollectionUpdater>(),
            songContextMenuVm);

        await searchVm.InitializeAsync();
        await songContextMenuVm.AddToNewPlaylistAsync("Fresh");

        contextMenuService.Verify(
            x => x.AddToNewPlaylistAsync(
                "Fresh",
                It.Is<IReadOnlyList<string>>(paths => paths.Contains("a.mp3") && paths.Contains("b.mp3")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleOpenedAsync_PlaylistHostNamedTab_ShowsActionsWithoutRemove()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var playlistMembership = new Mock<IPlaylistMembership>();
        playlistMembership
            .Setup(x => x.GetPlaylistMembershipInfoAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlaylistMembershipInfo(7, "Road Trip", true)]);

        var songContextMenuVm = new SongContextMenuViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            playlistMembership.Object,
            new SongContextSelectionService());

        var queueState = CreateQueueState();
        var pane = CreatePlaylistPane(
            logger.Object,
            messenger,
            queueState,
            songContextMenuVm,
            Mock.Of<IDefaultPlaylistService>(),
            Mock.Of<IPlaybackQueueActionsService>());

        await pane.InitializeAsync();
        var song = new AudioModel { Path = "named.mp3", Title = "Named track" };

        // Switch to a named playlist via sidebar selection
        messenger.Send(new PlaylistSidebarSelectionChangedMessage(new PlaylistSidebarSelectionData(7)));
        // Wait for async playlist load
        for (var i = 0; i < 20 && pane.CurrentPlaylistSongs.Count == 0; i++)
        {
            await Task.Delay(25);
        }

        pane.SelectedSong = song;

        await songContextMenuVm.HandleOpenedAsync();

        Assert.True(songContextMenuVm.ShowPlaylistActions);
        Assert.False(songContextMenuVm.ShowRemoveFromPlaylistAction);
    }

    [Fact]
    public async Task PlaylistHostDefaultTab_ActionsDispatchToPlaylistPane()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var playlistMembership = new Mock<IPlaylistMembership>();
        playlistMembership
            .Setup(x => x.GetPlaylistMembershipInfoAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlaylistMembershipInfo(12, "Workout", true)]);

        var defaultPlaylistService = new Mock<IDefaultPlaylistService>();
        var playbackQueueActionsService = new Mock<IPlaybackQueueActionsService>();
        playbackQueueActionsService
            .Setup(x => x.JumpToSelectedSongAsync())
            .Returns(Task.CompletedTask);
        var fileScanner = new Mock<IFileScanner>();
        fileScanner
            .Setup(x => x.ScanAsync("default.mp3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioModel { Path = "default.mp3", Title = "Default track (rescanned)" });

        var songContextMenuVm = new SongContextMenuViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            playlistMembership.Object,
            new SongContextSelectionService());

        var queueState = CreateQueueState();
        var song = new AudioModel { Path = "default.mp3", Title = "Default track" };
        queueState.DefaultPlaylist.Add(song);

        var pane = CreatePlaylistPane(
            logger.Object,
            messenger,
            queueState,
            songContextMenuVm,
            defaultPlaylistService.Object,
            playbackQueueActionsService.Object,
            fileScanner.Object);

        await pane.InitializeAsync();
        pane.SelectedSong = song;
        pane.SelectedIndex = 0;

        await songContextMenuVm.HandleOpenedAsync();
        await songContextMenuVm.RescanAsync();
        await songContextMenuVm.PlayAfterCurrentAsync();
        await songContextMenuVm.PlayNowAsync();
        await songContextMenuVm.RemoveFromPlaylistAsync();

        await AssertEventuallyAsync(() =>
            fileScanner.Invocations.Any(x => x.Method.Name == nameof(IFileScanner.ScanAsync)) &&
            playbackQueueActionsService.Invocations.Any(x => x.Method.Name == nameof(IPlaybackQueueActionsService.SetSelectedSongAsNext)) &&
            playbackQueueActionsService.Invocations.Any(x => x.Method.Name == nameof(IPlaybackQueueActionsService.JumpToSelectedSongAsync)));
        await AssertEventuallyAsync(() => defaultPlaylistService.Invocations.Any(x =>
            x.Method.Name == nameof(IDefaultPlaylistService.RemoveFromDefaultPlaylist)));

        Assert.True(songContextMenuVm.ShowPlaylistActions);
        Assert.True(songContextMenuVm.ShowRemoveFromPlaylistAction);
        fileScanner.Verify(x => x.ScanAsync("default.mp3", It.IsAny<CancellationToken>()), Times.Once);
        playbackQueueActionsService.Verify(x => x.SetSelectedSongAsNext(), Times.Once);
        playbackQueueActionsService.Verify(x => x.JumpToSelectedSongAsync(), Times.Once);
        defaultPlaylistService.Verify(
            x => x.RemoveFromDefaultPlaylist(It.Is<IEnumerable<AudioModel>>(songs => songs.Any(y => y.Path == "default.mp3"))),
            Times.Once);
    }

    private static Mock<ILogger> CreateLogger()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        return logger;
    }

    private static PlaylistQueueState CreateQueueState() => new(new PlaylistQueue());

    private static IAudioSearchExecutionService CreateAudioSearchService()
    {
        var mock = new Mock<IAudioSearchExecutionService>();
        mock.Setup(x => x.ExecuteQuickSearchAsync(It.IsAny<string>()))
            .ReturnsAsync(Array.Empty<AudioModel>());
        return mock.Object;
    }

    private static IAppSettingsReader CreateSettingsReader()
    {
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetFontFamily()).Returns("Segoe UI");
        settingsReader.Setup(x => x.GetMusicFolders()).Returns(Array.Empty<string>());
        settingsReader.Setup(x => x.GetMutedDroppedSongFolders()).Returns(Array.Empty<string>());
        settingsReader
            .Setup(x => x.GetSearchResultsTransferMode())
            .Returns(SearchResultsTransferMode.Move);
        return settingsReader.Object;
    }

    private static PlaylistPaneViewModel CreatePlaylistPane(
        ILogger logger,
        IMessenger messenger,
        PlaylistQueueState queueState,
        SongContextMenuViewModel songContextMenuViewModel,
        IDefaultPlaylistService defaultPlaylistService,
        IPlaybackQueueActionsService playbackQueueActionsService,
        IFileScanner? fileScanner = null)
    {
        var playlistLibraryService = new Mock<IPlaylistLibraryService>();
        playlistLibraryService
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlaylistSummary>());
        playlistLibraryService
            .Setup(x => x.GetPlaylistSongsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AudioModel { Path = "named.mp3", Title = "Named track" }]);

        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetFontFamily()).Returns("Segoe UI");
        settingsReader.Setup(x => x.GetUseCompactPlaylistView()).Returns(false);

        var routingService = new PlaylistQueueRoutingService(
            queueState,
            new PlaylistQueue(),
            Mock.Of<IMusicPlayerController>());

        var sidebarViewModel = new PlaylistSidebarViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            playlistLibraryService.Object,
            queueState);

        return new PlaylistPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            queueState,
            routingService,
            defaultPlaylistService,
            playbackQueueActionsService,
            Mock.Of<IExternalDropImportService>(),
            new PlaylistSelectionService(),
            playlistLibraryService.Object,
            new PlaybackContextSyncService(queueState),
            Mock.Of<IExternalAudioOpenService>(),
            Mock.Of<IExternalAudioOpenInbox>(),
            Mock.Of<IMusicPlayerController>(),
            fileScanner ?? Mock.Of<IFileScanner>(),
            new ObservableCollectionUpdater(),
            new PlaylistSortService(),
            settingsReader.Object,
            sidebarViewModel,
            songContextMenuViewModel);
    }

    private static async Task AssertEventuallyAsync(Func<bool> condition)
    {
        const int maxAttempts = 20;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(25);
        }

        Assert.True(condition());
    }
}
