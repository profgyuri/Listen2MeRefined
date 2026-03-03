using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Scanning.Files;
using Listen2MeRefined.Infrastructure.Searching;
using Listen2MeRefined.Infrastructure.Startup.ShellOpen;
using Listen2MeRefined.Infrastructure.Settings;
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
        var settingsReader = new Mock<IAppSettingsReader>();
        var pane = new PlaylistPaneViewModel(lists, settingsReader.Object);
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
        var playerController = new Mock<IMusicPlayerController>();
        var playlist = new Playlist();
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();

        return new ListsViewModel(
            logger.Object,
            mediator.Object,
            audioSearchExecutionService.Object,
            scanner.Object,
            playerController.Object,
            playlist,
            externalAudioOpenService.Object);
    }
}

