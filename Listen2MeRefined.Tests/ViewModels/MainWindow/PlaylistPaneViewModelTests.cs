using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.ContextMenus;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Playlist;
using Listen2MeRefined.Infrastructure.Startup;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels.MainWindow;

public class PlaylistPaneViewModelTests
{
    [Fact]
    public async Task CurrentSongChangedMessage_PropagatesSelectedSongChangeToPlaylistPane()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var queueServices = CreateQueueServices(logger.Object);
        var pane = CreatePane(logger.Object, messenger, queueServices);
        var song = new AudioModel { Title = "Current", Path = "song.mp3" };
        queueServices.State.PlayList.Add(song);
        await pane.InitializeAsync();

        var changed = false;
        pane.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PlaylistPaneViewModel.SelectedSong))
            {
                changed = true;
            }
        };

        messenger.Send(new CurrentSongChangedMessage(song));

        Assert.True(changed);
        Assert.Same(song, pane.SelectedSong);
    }

    [Fact]
    public async Task RemoveSelectedFromActiveTab_OnDefaultTabWithoutSelection_ClearsDefaultQueue()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var queueServices = CreateQueueServices(logger.Object);
        var pane = CreatePane(logger.Object, messenger, queueServices);

        var songOne = new AudioModel { Title = "One", Path = "one.mp3" };
        var songTwo = new AudioModel { Title = "Two", Path = "two.mp3" };
        queueServices.State.DefaultPlaylist.Add(songOne);
        queueServices.State.DefaultPlaylist.Add(songTwo);
        queueServices.RoutingService.ActivateDefaultPlaylistQueue();

        await pane.InitializeAsync();
        await pane.RemoveSelectedFromActiveTabCommand.ExecuteAsync(null);

        Assert.Empty(queueServices.State.DefaultPlaylist);
        Assert.Empty(queueServices.State.PlayList);
    }

    [Fact]
    public async Task RemoveSelectedFromDefaultPlaylistSelectionAsync_WithSelection_RemovesOnlySelectedSong()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var queueServices = CreateQueueServices(logger.Object);
        var pane = CreatePane(logger.Object, messenger, queueServices);

        var keepSong = new AudioModel { Title = "Keep", Path = "keep.mp3" };
        var removeSong = new AudioModel { Title = "Remove", Path = "remove.mp3" };
        queueServices.State.DefaultPlaylist.Add(keepSong);
        queueServices.State.DefaultPlaylist.Add(removeSong);
        queueServices.RoutingService.ActivateDefaultPlaylistQueue();

        await pane.InitializeAsync();
        pane.SelectedSong = removeSong;

        await pane.RemoveSelectedFromDefaultPlaylistSelectionAsync();

        Assert.Single(queueServices.State.DefaultPlaylist);
        Assert.Contains(keepSong, queueServices.State.DefaultPlaylist);
        Assert.DoesNotContain(removeSong, queueServices.State.DefaultPlaylist);
    }

    [Fact]
    public async Task RemoveSelectedFromDefaultPlaylistSelectionAsync_WithoutSelection_DoesNotClearDefaultQueue()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var queueServices = CreateQueueServices(logger.Object);
        var pane = CreatePane(logger.Object, messenger, queueServices);

        var first = new AudioModel { Title = "First", Path = "first.mp3" };
        var second = new AudioModel { Title = "Second", Path = "second.mp3" };
        queueServices.State.DefaultPlaylist.Add(first);
        queueServices.State.DefaultPlaylist.Add(second);
        queueServices.RoutingService.ActivateDefaultPlaylistQueue();

        await pane.InitializeAsync();
        pane.SelectedSong = null;

        await pane.RemoveSelectedFromDefaultPlaylistSelectionAsync();

        Assert.Equal(2, queueServices.State.DefaultPlaylist.Count);
        Assert.Contains(first, queueServices.State.DefaultPlaylist);
        Assert.Contains(second, queueServices.State.DefaultPlaylist);
    }

    [Fact]
    public async Task SetSelectedSongAsNextCommand_DefaultTab_RefreshesVisibleOrder()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var queueServices = CreateQueueServices(logger.Object);
        var pane = CreatePane(logger.Object, messenger, queueServices);

        var first = new AudioModel { Title = "A", Path = "a.mp3" };
        var second = new AudioModel { Title = "B", Path = "b.mp3" };
        var third = new AudioModel { Title = "C", Path = "c.mp3" };
        var fourth = new AudioModel { Title = "D", Path = "d.mp3" };
        queueServices.State.DefaultPlaylist.Add(first);
        queueServices.State.DefaultPlaylist.Add(second);
        queueServices.State.DefaultPlaylist.Add(third);
        queueServices.State.DefaultPlaylist.Add(fourth);
        queueServices.RoutingService.ActivateDefaultPlaylistQueue();
        queueServices.State.CurrentSongIndex = 2;

        await pane.InitializeAsync();
        pane.SelectedSong = first;

        await pane.SetSelectedSongAsNextCommand.ExecuteAsync(null);

        Assert.Equal(["B", "C", "A", "D"], pane.CurrentPlaylistSongs.Select(x => x.Title ?? string.Empty).ToArray());
    }

    [Fact]
    public async Task SetSelectedSongAsNextCommand_NamedPlaylist_RefreshesVisibleOrder()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var queueServices = CreateQueueServices(logger.Object);

        var first = new AudioModel { Title = "A", Path = "a.mp3" };
        var second = new AudioModel { Title = "B", Path = "b.mp3" };
        var third = new AudioModel { Title = "C", Path = "c.mp3" };
        var fourth = new AudioModel { Title = "D", Path = "d.mp3" };

        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        playlistLibrary
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlaylistSummary(42, "Named", false, 4)]);
        playlistLibrary
            .Setup(x => x.GetPlaylistSongsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second, third, fourth]);

        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetUseCompactPlaylistView()).Returns(false);

        var sidebarViewModel = new PlaylistSidebarViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            playlistLibrary.Object,
            queueServices.State);

        var pane = new PlaylistPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            queueServices.State,
            queueServices.RoutingService,
            queueServices.DefaultPlaylistService,
            queueServices.PlaybackActionsService,
            queueServices.DropImportService,
            new PlaylistSelectionService(),
            playlistLibrary.Object,
            queueServices.PlaybackContextSyncService,
            Mock.Of<IExternalAudioOpenService>(),
            Mock.Of<IExternalAudioOpenInbox>(),
            settingsReader.Object,
            sidebarViewModel,
            CreateSongContextMenuViewModel(logger.Object, messenger));

        await pane.InitializeAsync();

        // Select the named playlist via sidebar message
        messenger.Send(new PlaylistSidebarSelectionChangedMessage(new PlaylistSidebarSelectionData(42)));

        // Wait for async loading of playlist songs
        for (var i = 0; i < 20 && pane.CurrentPlaylistSongs.Count == 0; i++)
        {
            await Task.Delay(25);
        }

        queueServices.RoutingService.ActivateNamedPlaylistQueue(42, pane.CurrentPlaylistSongs);
        queueServices.State.CurrentSongIndex = 2;
        pane.SelectedSong = first;

        await pane.SetSelectedSongAsNextCommand.ExecuteAsync(null);

        Assert.Equal(["B", "C", "A", "D"], pane.CurrentPlaylistSongs.Select(x => x.Title ?? string.Empty).ToArray());
    }

    [Fact]
    public async Task InitializeAsync_LoadsCompactViewModeFromSettings()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var queueServices = CreateQueueServices(logger.Object);
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetUseCompactPlaylistView()).Returns(true);
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        playlistLibrary
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlaylistSummary>());

        var sidebarViewModel = new PlaylistSidebarViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            playlistLibrary.Object,
            queueServices.State);

        var pane = new PlaylistPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            queueServices.State,
            queueServices.RoutingService,
            queueServices.DefaultPlaylistService,
            queueServices.PlaybackActionsService,
            queueServices.DropImportService,
            new PlaylistSelectionService(),
            playlistLibrary.Object,
            queueServices.PlaybackContextSyncService,
            Mock.Of<IExternalAudioOpenService>(),
            Mock.Of<IExternalAudioOpenInbox>(),
            settingsReader.Object,
            sidebarViewModel,
            CreateSongContextMenuViewModel(logger.Object, messenger));

        await pane.InitializeAsync();

        Assert.True(pane.IsCompactPlaylistView);
        settingsReader.Verify(x => x.GetUseCompactPlaylistView(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SearchResultsToPlaylistRequestedMessage_AddsSongsToDefaultPlaylist()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var queueServices = CreateQueueServices(logger.Object);
        var pane = CreatePane(logger.Object, messenger, queueServices);
        await pane.InitializeAsync();

        var first = new AudioModel { Path = "a.mp3", Title = "A" };
        var second = new AudioModel { Path = "b.mp3", Title = "B" };
        messenger.Send(new SearchResultsToPlaylistRequestedMessage([first, second]));

        Assert.Equal(2, queueServices.State.DefaultPlaylist.Count);
        Assert.Equal(2, queueServices.State.PlayList.Count);
    }

    [Fact]
    public async Task PlaylistViewModeChangedMessage_UpdatesCompactViewMode()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var queueServices = CreateQueueServices(logger.Object);
        var pane = CreatePane(logger.Object, messenger, queueServices);
        await pane.InitializeAsync();

        Assert.False(pane.IsCompactPlaylistView);

        messenger.Send(new PlaylistViewModeChangedMessage(true));

        Assert.True(pane.IsCompactPlaylistView);
    }

    [Fact]
    public async Task ExternalAudioOpenInbox_LivePaths_ForwardedToExternalAudioOpenService()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var queueServices = CreateQueueServices(logger.Object);
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();
        var externalAudioOpenInbox = new ExternalAudioOpenInbox(logger.Object);
        var pane = CreatePane(
            logger.Object,
            messenger,
            queueServices,
            externalAudioOpenService.Object,
            externalAudioOpenInbox);
        await pane.InitializeAsync();

        var paths = new[] { "a.mp3", "b.mp3" };
        externalAudioOpenInbox.Enqueue(paths);

        for (var i = 0; i < 20; i++)
        {
            if (externalAudioOpenService.Invocations.Count > 0)
            {
                break;
            }

            await Task.Delay(25);
        }

        externalAudioOpenService.Verify(
            x => x.OpenAsync(
                It.Is<IReadOnlyList<string>>(value => value.SequenceEqual(paths)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExternalAudioOpenInbox_PathsQueuedBeforeInitialization_AreReplayedToExternalAudioOpenService()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var queueServices = CreateQueueServices(logger.Object);
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();
        var externalAudioOpenInbox = new ExternalAudioOpenInbox(logger.Object);
        var pane = CreatePane(
            logger.Object,
            messenger,
            queueServices,
            externalAudioOpenService.Object,
            externalAudioOpenInbox);

        var paths = new[] { "pre-init-a.mp3", "pre-init-b.mp3" };
        externalAudioOpenInbox.Enqueue(paths);

        await pane.InitializeAsync();

        for (var i = 0; i < 20; i++)
        {
            if (externalAudioOpenService.Invocations.Count > 0)
            {
                break;
            }

            await Task.Delay(25);
        }

        externalAudioOpenService.Verify(
            x => x.OpenAsync(
                It.Is<IReadOnlyList<string>>(value => value.SequenceEqual(paths)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JumpToSelectedSongCommand_UsesCoordinatorSelectionAndJumps()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var playerController = new Mock<IMusicPlayerController>();
        var queueServices = CreateQueueServices(logger.Object, playerController: playerController);
        var pane = CreatePane(logger.Object, messenger, queueServices);
        var first = new AudioModel { Title = "First", Path = "first.mp3" };
        var second = new AudioModel { Title = "Second", Path = "second.mp3" };
        queueServices.State.PlayList.Add(first);
        queueServices.State.PlayList.Add(second);

        Assert.False(pane.JumpToSelectedSongCommand.CanExecute(null));

        pane.SelectedSong = second;
        pane.SelectedIndex = 1;

        Assert.True(pane.JumpToSelectedSongCommand.CanExecute(null));
        await pane.JumpToSelectedSongCommand.ExecuteAsync(null);
        playerController.Verify(x => x.JumpToIndexAsync(1), Times.Once);
    }

    private static PlaylistPaneViewModel CreatePane(
        ILogger logger,
        IMessenger messenger,
        QueueServices queueServices,
        IExternalAudioOpenService? externalAudioOpenService = null,
        IExternalAudioOpenInbox? externalAudioOpenInbox = null)
    {
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        playlistLibrary
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlaylistSummary>());
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetUseCompactPlaylistView()).Returns(false);

        var sidebarViewModel = new PlaylistSidebarViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            playlistLibrary.Object,
            queueServices.State);

        return new PlaylistPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            queueServices.State,
            queueServices.RoutingService,
            queueServices.DefaultPlaylistService,
            queueServices.PlaybackActionsService,
            queueServices.DropImportService,
            new PlaylistSelectionService(),
            playlistLibrary.Object,
            queueServices.PlaybackContextSyncService,
            externalAudioOpenService ?? Mock.Of<IExternalAudioOpenService>(),
            externalAudioOpenInbox ?? Mock.Of<IExternalAudioOpenInbox>(),
            settingsReader.Object,
            sidebarViewModel,
            CreateSongContextMenuViewModel(logger, messenger));
    }

    private static QueueServices CreateQueueServices(
        ILogger logger,
        Mock<IMusicPlayerController>? playerController = null,
        Mock<IFileScanner>? fileScanner = null,
        Mock<IAppSettingsReader>? settingsReader = null,
        Mock<IAppSettingsWriter>? settingsWriter = null,
        Mock<IDroppedSongFolderPromptService>? promptService = null)
    {
        var playlistQueue = new PlaylistQueue();
        var queueState = new PlaylistQueueState(playlistQueue);
        var musicPlayerController = playerController ?? new Mock<IMusicPlayerController>();
        var scanner = fileScanner ?? new Mock<IFileScanner>();
        var reader = settingsReader ?? new Mock<IAppSettingsReader>();
        var writer = settingsWriter ?? new Mock<IAppSettingsWriter>();
        var prompt = promptService ?? new Mock<IDroppedSongFolderPromptService>();

        reader.Setup(x => x.GetMusicFolders()).Returns(Array.Empty<string>());
        reader.Setup(x => x.GetMutedDroppedSongFolders()).Returns(Array.Empty<string>());
        prompt.Setup(x => x.PromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddDroppedSongFolderDecision.Skip);

        var playbackContextSync = new PlaybackContextSyncService(queueState);
        return new QueueServices(
            queueState,
            new DefaultPlaylistService(queueState),
            new PlaylistQueueRoutingService(queueState, playlistQueue, musicPlayerController.Object),
            playbackContextSync,
            new PlaybackQueueActionsService(
                queueState,
                playbackContextSync,
                scanner.Object,
                musicPlayerController.Object,
                logger),
            new ExternalDropImportService(
                queueState,
                scanner.Object,
                reader.Object,
                writer.Object,
                prompt.Object));
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
        IPlaybackQueueActionsService PlaybackActionsService,
        IExternalDropImportService DropImportService);
}
