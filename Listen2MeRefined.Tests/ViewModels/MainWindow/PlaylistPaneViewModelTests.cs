using System.Collections;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.ContextMenus;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using MediatR;
using Moq;
using Serilog;
using ListsViewModel = Listen2MeRefined.Application.ViewModels.Widgets.ListsViewModel;
using PlaylistPaneViewModel = Listen2MeRefined.Application.ViewModels.Widgets.PlaylistPaneViewModel;

namespace Listen2MeRefined.Tests.ViewModels.MainWindow;

public class PlaylistPaneViewModelTests
{
    [Fact]
    public async Task CurrentSongNotification_PropagatesSelectedSongChangeToPlaylistPane()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        var messenger = new Mock<IMessenger>();
        var lists = CreateListsViewModel();
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        playlistLibrary
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlaylistSummary>());
        var settingsReader = new Mock<IAppSettingsReader>();
        var pane = new PlaylistPaneViewModel(Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger.Object,
            lists, 
            playlistLibrary.Object, 
            Mock.Of<IMediator>(), 
            settingsReader.Object,
            CreateSongContextMenuViewModel(logger.Object, messenger.Object));
        var song = new AudioModel { Title = "Current", Path = "song.mp3" };
        lists.PlayList.Add(song);

        var changed = false;
        pane.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PlaylistPaneViewModel.SelectedSong))
            {
                changed = true;
            }
        };

        await lists.Handle(new CurrentSongNotification(song), CancellationToken.None);

        Assert.True(changed);
        Assert.Same(song, pane.SelectedSong);
    }

    [Fact]
    public async Task RemoveSelectedFromActiveTab_OnDefaultTabWithoutSelection_ClearsDefaultQueue()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        var messenger = new Mock<IMessenger>();
        var lists = CreateListsViewModel();
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        playlistLibrary
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlaylistSummary>());

        var songOne = new AudioModel { Title = "One", Path = "one.mp3" };
        var songTwo = new AudioModel { Title = "Two", Path = "two.mp3" };
        lists.SearchResults.Add(songOne);
        lists.SearchResults.Add(songTwo);
        lists.SendSelectedToPlaylistCommand.Execute(null);

        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetUseCompactPlaylistView()).Returns(false);
        var pane = new PlaylistPaneViewModel(Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger.Object,
            lists, 
            playlistLibrary.Object, 
            Mock.Of<IMediator>(), 
            settingsReader.Object,
            CreateSongContextMenuViewModel(logger.Object, messenger.Object));
        await pane.InitializeAsync();
        await pane.RemoveSelectedFromActiveTabCommand.ExecuteAsync(null);

        Assert.Empty(lists.DefaultPlaylist);
        Assert.Empty(lists.PlayList);
    }

    [Fact]
    public async Task HandlePlaylistCreatedNotification_AddsAndSelectsNewTab()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        var messenger = new Mock<IMessenger>();
        var lists = CreateListsViewModel();
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        playlistLibrary
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlaylistSummary(42, "Road Trip")]);
        playlistLibrary
            .Setup(x => x.GetPlaylistSongsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AudioModel { Title = "Song", Path = "song.mp3" }]);

        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetUseCompactPlaylistView()).Returns(false);
        var pane = new PlaylistPaneViewModel(Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger.Object,
            lists, 
            playlistLibrary.Object, 
            Mock.Of<IMediator>(), 
            settingsReader.Object,
            CreateSongContextMenuViewModel(logger.Object, messenger.Object));
        await pane.InitializeAsync();
        await pane.Handle(new PlaylistCreatedNotification(42, "Road Trip"), CancellationToken.None);

        Assert.Equal(2, pane.Tabs.Count);
        Assert.Equal(42, pane.SelectedTab?.PlaylistId);
        Assert.Equal("Road Trip", pane.SelectedTab?.Header);
    }

    [Fact]
    public async Task InitializeAsync_LoadsCompactViewModeFromSettings()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        var messenger = new Mock<IMessenger>();
        var lists = CreateListsViewModel();
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetUseCompactPlaylistView()).Returns(true);
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        playlistLibrary
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlaylistSummary>());
        var pane = new PlaylistPaneViewModel(Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger.Object,
            lists, 
            playlistLibrary.Object, 
            Mock.Of<IMediator>(), 
            settingsReader.Object,
            CreateSongContextMenuViewModel(logger.Object, messenger.Object));

        await pane.InitializeAsync();

        Assert.True(pane.IsCompactPlaylistView);
        settingsReader.Verify(x => x.GetUseCompactPlaylistView(), Times.AtLeastOnce);
    }

    private static ListsViewModel CreateListsViewModel()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        var mediator = new Mock<IMediator>();
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        var scanner = new Mock<IFileScanner>();
        var settingsReader = new Mock<IAppSettingsReader>();
        var playerController = new Mock<IMusicPlayerController>();
        var playlist = new Playlist();
        settingsReader.Setup(x => x.GetMusicFolders()).Returns(Array.Empty<string>());
        settingsReader.Setup(x => x.GetMutedDroppedSongFolders()).Returns(Array.Empty<string>());
        var settingsWriter = new Mock<IAppSettingsWriter>();
        var prompt = new Mock<IDroppedSongFolderPromptService>();
        prompt.Setup(x => x.PromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddDroppedSongFolderDecision.Skip);
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();
        settingsReader
            .Setup(x => x.GetSearchResultsTransferMode())
            .Returns(SearchResultsTransferMode.Move);
        var ui = new Mock<IUiDispatcher>();

        return new ListsViewModel(
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
    }

    private static SongContextMenuViewModel CreateSongContextMenuViewModel(ILogger logger, IMessenger messenger)
    {
        return new SongContextMenuViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            Mock.Of<ISongContextMenuService>(),
            Mock.Of<ISongContextSelectionService>());
    }
}

