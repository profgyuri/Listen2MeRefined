using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Playlist;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Playlist;

public class PlaybackQueueActionsServiceTests
{
    [Fact]
    public void SetSelectedSongAsNext_SelectedSongBeforeCurrent_InsertsDirectlyAfterCurrentSong()
    {
        var songs = CreateSongs("A", "B", "C", "D");
        var service = CreateService(songs, currentSongIndex: 2, selectedSongIndex: 0, out var queueState);

        service.SetSelectedSongAsNext();

        Assert.Equal(["B", "C", "A", "D"], queueState.PlayList.Select(x => x.Title ?? string.Empty).ToArray());
        Assert.Equal(1, queueState.CurrentSongIndex);
        Assert.Equal(2, queueState.SelectedIndex);
    }

    [Fact]
    public void SetSelectedSongAsNext_CurrentSongIsLast_WrapsSelectionToQueueStart()
    {
        var songs = CreateSongs("A", "B", "C", "D");
        var service = CreateService(songs, currentSongIndex: 3, selectedSongIndex: 1, out var queueState);

        service.SetSelectedSongAsNext();

        Assert.Equal(["B", "A", "C", "D"], queueState.PlayList.Select(x => x.Title ?? string.Empty).ToArray());
        Assert.Equal(3, queueState.CurrentSongIndex);
        Assert.Equal(0, queueState.SelectedIndex);
    }

    [Fact]
    public void SetSelectedSongAsNext_SelectedSongIsCurrentSong_DoesNotReorder()
    {
        var songs = CreateSongs("A", "B", "C");
        var service = CreateService(songs, currentSongIndex: 1, selectedSongIndex: 1, out var queueState);

        service.SetSelectedSongAsNext();

        Assert.Equal(["A", "B", "C"], queueState.PlayList.Select(x => x.Title ?? string.Empty).ToArray());
        Assert.Equal(1, queueState.CurrentSongIndex);
        Assert.Equal(1, queueState.SelectedIndex);
    }

    [Fact]
    public void SetSelectedSongAsNext_SelectedSongHasDifferentReferenceButSamePath_ReordersByPath()
    {
        var songs = CreateSongs("A", "B", "C", "D");
        var service = CreateService(songs, currentSongIndex: 2, selectedSongIndex: 0, out var queueState);
        queueState.SelectedSong = new AudioModel { Path = "A.mp3", Title = "A (copy)" };

        service.SetSelectedSongAsNext();

        Assert.Equal(["B", "C", "A", "D"], queueState.PlayList.Select(x => x.Title ?? string.Empty).ToArray());
        Assert.Equal(1, queueState.CurrentSongIndex);
        Assert.Equal(2, queueState.SelectedIndex);
        Assert.Equal("A", queueState.SelectedSong?.Title);
    }

    private static PlaybackQueueActionsService CreateService(
        IReadOnlyList<AudioModel> songs,
        int currentSongIndex,
        int selectedSongIndex,
        out PlaylistQueueState queueState)
    {
        var playlistQueue = new PlaylistQueue();
        foreach (var song in songs)
        {
            playlistQueue.Items.Add(song);
        }

        queueState = new PlaylistQueueState(playlistQueue)
        {
            CurrentSongIndex = currentSongIndex,
            SelectedIndex = selectedSongIndex,
            SelectedSong = songs[selectedSongIndex]
        };

        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        return new PlaybackQueueActionsService(
            queueState,
            new PlaybackContextSyncService(queueState),
            Mock.Of<IFileScanner>(),
            Mock.Of<IMusicPlayerController>(),
            logger.Object);
    }

    private static AudioModel[] CreateSongs(params string[] titles) =>
        titles.Select(x => new AudioModel { Title = x, Path = $"{x}.mp3" }).ToArray();
}
