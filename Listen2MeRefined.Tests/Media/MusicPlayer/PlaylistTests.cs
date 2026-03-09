using System.Collections.ObjectModel;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;

namespace Listen2MeRefined.Tests.Media.MusicPlayer;

public class PlaylistTests
{
    [Fact]
    public void Move_WhenCurrentItemIsMoved_UpdatesCurrentIndexToNewPosition()
    {
        var playlist = CreatePlaylistWith3Tracks();
        playlist.CurrentIndex = 1;

        playlist.Move(1, 2);

        Assert.Equal(2, playlist.CurrentIndex);
    }

    [Fact]
    public void Move_WhenItemBeforeCurrentMovesAfterCurrent_DecrementsCurrentIndex()
    {
        var playlist = CreatePlaylistWith3Tracks();
        playlist.CurrentIndex = 2;

        playlist.Move(0, 2);

        Assert.Equal(1, playlist.CurrentIndex);
    }

    [Fact]
    public void DragDropMoveViaObservableCollection_WhenItemAfterCurrentMovesBeforeCurrent_IncrementsCurrentIndex()
    {
        var playlist = CreatePlaylistWith3Tracks();
        playlist.CurrentIndex = 0;
        var items = (ObservableCollection<AudioModel>)playlist.Items;

        items.Move(2, 0);

        Assert.Equal(1, playlist.CurrentIndex);
    }

    private static Playlist CreatePlaylistWith3Tracks()
    {
        var playlist = new Playlist();
        playlist.Items.Add(new AudioModel { Title = "A", Path = "a" });
        playlist.Items.Add(new AudioModel { Title = "B", Path = "b" });
        playlist.Items.Add(new AudioModel { Title = "C", Path = "c" });
        return playlist;
    }
}

