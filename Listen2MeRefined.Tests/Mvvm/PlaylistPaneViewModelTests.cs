using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Data.Repositories;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Mvvm.MainWindow;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Services.Contracts;
using MediatR;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Mvvm;

public class PlaylistPaneViewModelTests
{
    [Fact]
    public async Task CurrentSongNotification_PropagatesSelectedSongChangeToPlaylistPane()
    {
        var lists = CreateListsViewModel();
        var pane = new PlaylistPaneViewModel(lists);
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

    private static ListsViewModel CreateListsViewModel()
    {
        var logger = new Mock<ILogger>();
        var mediator = new Mock<IMediator>();
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        var scanner = new Mock<IFileScanner>();
        var playerController = new Mock<IMusicPlayerController>();
        var playlist = new Playlist();

        return new ListsViewModel(
            logger.Object,
            mediator.Object,
            audioSearchExecutionService.Object,
            scanner.Object,
            playerController.Object,
            playlist);
    }
}
