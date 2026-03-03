using System.Collections;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Playlist;
using Listen2MeRefined.Infrastructure.Scanning.Files;
using Listen2MeRefined.Infrastructure.Searching;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.Infrastructure.Startup.ShellOpen;
using MediatR;
using Moq;
using Serilog;
using ListsViewModel = Listen2MeRefined.Infrastructure.ViewModels.MainWindow.ListsViewModel;
using PlaylistPaneViewModel = Listen2MeRefined.Infrastructure.ViewModels.MainWindow.PlaylistPaneViewModel;

namespace Listen2MeRefined.Tests.ViewModels.MainWindow;

public class PlaylistPaneViewModelTests
{
    [Fact]
    public async Task CurrentSongNotification_PropagatesSelectedSongChangeToPlaylistPane()
    {
        var lists = CreateListsViewModel();
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        playlistLibrary
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlaylistSummary>());
        var settingsReader = new Mock<IAppSettingsReader>();
        var pane = new PlaylistPaneViewModel(lists, playlistLibrary.Object, Mock.Of<IMediator>(), settingsReader.Object);
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

        var pane = new PlaylistPaneViewModel(lists, playlistLibrary.Object, Mock.Of<IMediator>());
        await pane.RemoveSelectedFromActiveTabCommand.ExecuteAsync(null);

        Assert.Empty(lists.DefaultPlaylist);
        Assert.Empty(lists.PlayList);
    }

    [Fact]
    public async Task HandlePlaylistCreatedNotification_AddsAndSelectsNewTab()
    {
        var lists = CreateListsViewModel();
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        playlistLibrary
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlaylistSummary(42, "Road Trip")]);
        playlistLibrary
            .Setup(x => x.GetPlaylistSongsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AudioModel { Title = "Song", Path = "song.mp3" }]);

        var pane = new PlaylistPaneViewModel(lists, playlistLibrary.Object, Mock.Of<IMediator>());
        await pane.Handle(new PlaylistCreatedNotification(42, "Road Trip"), CancellationToken.None);

        Assert.Equal(2, pane.Tabs.Count);
        Assert.Equal(42, pane.SelectedTab?.PlaylistId);
        Assert.Equal("Road Trip", pane.SelectedTab?.Header);
    }

    [Fact]
    public async Task AddToNewPlaylistFromContextAsync_UsesEverySelectedSongPath()
    {
        var lists = CreateListsViewModel();
        var mediator = new Mock<IMediator>();
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        playlistLibrary
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlaylistSummary>());
        playlistLibrary
            .Setup(x => x.CreatePlaylistAsync("Fresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlaylistSummary(15, "Fresh"));

        var pane = new PlaylistPaneViewModel(lists, playlistLibrary.Object, mediator.Object);
        var first = new AudioModel { Title = "First", Path = "a.mp3" };
        var second = new AudioModel { Title = "Second", Path = "b.mp3" };
        lists.DefaultPlaylist.Add(first);
        lists.DefaultPlaylist.Add(second);
        lists.ActivateDefaultPlaylistQueue();

        pane.PlaylistSelectionAddedCommand.Execute(new ArrayList { first, second });
        await pane.AddToNewPlaylistFromContextAsync("Fresh");

        playlistLibrary.Verify(
            x => x.AddSongsByPathAsync(
                15,
                It.Is<IEnumerable<string?>>(paths => paths.Contains("a.mp3") && paths.Contains("b.mp3")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mediator.Verify(
            x => x.Publish(It.IsAny<PlaylistCreatedNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mediator.Verify(
            x => x.Publish(It.IsAny<PlaylistMembershipChangedNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_LoadsCompactViewModeFromSettings()
    {
        var lists = CreateListsViewModel();
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetUseCompactPlaylistView()).Returns(true);
        var pane = new PlaylistPaneViewModel(lists, settingsReader.Object);

        await pane.InitializeAsync();

        Assert.True(pane.IsCompactPlaylistView);
        settingsReader.Verify(x => x.GetUseCompactPlaylistView(), Times.Once);
    }

    private static ListsViewModel CreateListsViewModel()
    {
        var logger = new Mock<ILogger>();
        var mediator = new Mock<IMediator>();
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        var scanner = new Mock<IFileScanner>();
        var settingsReader = new Mock<IAppSettingsReader>();
        var playerController = new Mock<IMusicPlayerController>();
        var playlist = new Playlist();
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();
        settingsReader
            .Setup(x => x.GetSearchResultsTransferMode())
            .Returns(SearchResultsTransferMode.Move);

        return new ListsViewModel(
            logger.Object,
            mediator.Object,
            audioSearchExecutionService.Object,
            scanner.Object,
            settingsReader.Object,
            playerController.Object,
            playlist,
            externalAudioOpenService.Object);
    }
}

