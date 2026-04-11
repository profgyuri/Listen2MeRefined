using System.Collections.ObjectModel;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Tests.Playlist;

public class PlaylistSortServiceTests
{
    private readonly PlaylistSortService _service = new();

    [Fact]
    public void Sort_ByTitle_Ascending_SortsSongsAlphabetically()
    {
        var songs = CreateSongs(("Cherry", "c.mp3"), ("Apple", "a.mp3"), ("Banana", "b.mp3"));

        _service.Sort(songs, PlaylistSortProperty.Title, SortDirection.Ascending);

        Assert.Equal(["Apple", "Banana", "Cherry"], Titles(songs));
    }

    [Fact]
    public void Sort_ByTitle_Descending_ReversesOrder()
    {
        var songs = CreateSongs(("Apple", "a.mp3"), ("Banana", "b.mp3"), ("Cherry", "c.mp3"));

        _service.Sort(songs, PlaylistSortProperty.Title, SortDirection.Descending);

        Assert.Equal(["Cherry", "Banana", "Apple"], Titles(songs));
    }

    [Fact]
    public void Sort_ByArtist_Ascending_SortsByArtistName()
    {
        var songs = new ObservableCollection<AudioModel>
        {
            new() { Artist = "Zephyr", Title = "S1", Path = "z.mp3" },
            new() { Artist = "Muse", Title = "S2", Path = "m.mp3" },
            new() { Artist = "ABBA", Title = "S3", Path = "a.mp3" }
        };

        _service.Sort(songs, PlaylistSortProperty.Artist, SortDirection.Ascending);

        Assert.Equal(["ABBA", "Muse", "Zephyr"], songs.Select(x => x.Artist ?? string.Empty).ToArray());
    }

    [Fact]
    public void Sort_ByDuration_Ascending_SortsByLength()
    {
        var songs = new ObservableCollection<AudioModel>
        {
            new() { Title = "Long", Path = "l.mp3", Length = TimeSpan.FromMinutes(5) },
            new() { Title = "Short", Path = "s.mp3", Length = TimeSpan.FromMinutes(1) },
            new() { Title = "Medium", Path = "m.mp3", Length = TimeSpan.FromMinutes(3) }
        };

        _service.Sort(songs, PlaylistSortProperty.Duration, SortDirection.Ascending);

        Assert.Equal(["Short", "Medium", "Long"], Titles(songs));
    }

    [Fact]
    public void Sort_ByGenre_Ascending_SortsByGenreName()
    {
        var songs = new ObservableCollection<AudioModel>
        {
            new() { Title = "R", Genre = "Rock", Path = "r.mp3" },
            new() { Title = "J", Genre = "Jazz", Path = "j.mp3" },
            new() { Title = "P", Genre = "Pop", Path = "p.mp3" }
        };

        _service.Sort(songs, PlaylistSortProperty.Genre, SortDirection.Ascending);

        Assert.Equal(["Jazz", "Pop", "Rock"], songs.Select(x => x.Genre ?? string.Empty).ToArray());
    }

    [Fact]
    public void Sort_ByBPM_Ascending_SortsByBeatsPerMinute()
    {
        var songs = new ObservableCollection<AudioModel>
        {
            new() { Title = "Fast", BPM = 180, Path = "f.mp3" },
            new() { Title = "Slow", BPM = 60, Path = "s.mp3" },
            new() { Title = "Mid", BPM = 120, Path = "m.mp3" }
        };

        _service.Sort(songs, PlaylistSortProperty.BPM, SortDirection.Ascending);

        Assert.Equal(["Slow", "Mid", "Fast"], Titles(songs));
    }

    [Fact]
    public void Sort_ByBitrate_Ascending_SortsByBitrate()
    {
        var songs = new ObservableCollection<AudioModel>
        {
            new() { Title = "High", Bitrate = 320, Path = "h.mp3" },
            new() { Title = "Low", Bitrate = 128, Path = "l.mp3" },
            new() { Title = "Med", Bitrate = 192, Path = "m.mp3" }
        };

        _service.Sort(songs, PlaylistSortProperty.Bitrate, SortDirection.Ascending);

        Assert.Equal(["Low", "Med", "High"], Titles(songs));
    }

    [Fact]
    public void Sort_EmptyCollection_DoesNotThrow()
    {
        var songs = new ObservableCollection<AudioModel>();

        _service.Sort(songs, PlaylistSortProperty.Title, SortDirection.Ascending);

        Assert.Empty(songs);
    }

    [Fact]
    public void Sort_SingleItem_DoesNotThrow()
    {
        var songs = new ObservableCollection<AudioModel>
        {
            new() { Title = "Only", Path = "only.mp3" }
        };

        _service.Sort(songs, PlaylistSortProperty.Title, SortDirection.Ascending);

        Assert.Single(songs);
        Assert.Equal("Only", songs[0].Title);
    }

    [Fact]
    public void Sort_NullStringProperties_TreatedAsEmptyString()
    {
        var withArtist = new AudioModel { Artist = "Muse", Title = "S1", Path = "m.mp3" };
        var nullArtist = new AudioModel { Artist = null, Title = "S2", Path = "n.mp3" };
        var songs = new ObservableCollection<AudioModel> { withArtist, nullArtist };

        _service.Sort(songs, PlaylistSortProperty.Artist, SortDirection.Ascending);

        Assert.Same(nullArtist, songs[0]);
        Assert.Same(withArtist, songs[1]);
    }

    [Fact]
    public void Sort_PreservesSameCollectionInstance()
    {
        var songs = CreateSongs(("B", "b.mp3"), ("A", "a.mp3"));
        var reference = songs;

        _service.Sort(songs, PlaylistSortProperty.Title, SortDirection.Ascending);

        Assert.Same(reference, songs);
    }

    [Fact]
    public void Sort_AlreadySorted_DoesNotChangeOrder()
    {
        var songs = CreateSongs(("Apple", "a.mp3"), ("Banana", "b.mp3"), ("Cherry", "c.mp3"));

        _service.Sort(songs, PlaylistSortProperty.Title, SortDirection.Ascending);

        Assert.Equal(["Apple", "Banana", "Cherry"], Titles(songs));
    }

    [Fact]
    public void Sort_DuplicateValues_HandledWithoutError()
    {
        var songs = new ObservableCollection<AudioModel>
        {
            new() { Artist = "Same", Title = "B", Path = "b.mp3" },
            new() { Artist = "Same", Title = "A", Path = "a.mp3" },
            new() { Artist = "Same", Title = "C", Path = "c.mp3" }
        };

        _service.Sort(songs, PlaylistSortProperty.Artist, SortDirection.Ascending);

        Assert.Equal(3, songs.Count);
        Assert.All(songs, s => Assert.Equal("Same", s.Artist));
    }

    [Fact]
    public void Sort_DescendingThenAscending_ReversesCorrectly()
    {
        var songs = CreateSongs(("Banana", "b.mp3"), ("Apple", "a.mp3"), ("Cherry", "c.mp3"));

        _service.Sort(songs, PlaylistSortProperty.Title, SortDirection.Descending);
        Assert.Equal(["Cherry", "Banana", "Apple"], Titles(songs));

        _service.Sort(songs, PlaylistSortProperty.Title, SortDirection.Ascending);
        Assert.Equal(["Apple", "Banana", "Cherry"], Titles(songs));
    }

    private static ObservableCollection<AudioModel> CreateSongs(params (string Title, string Path)[] items) =>
        new(items.Select(i => new AudioModel { Title = i.Title, Path = i.Path }));

    private static string[] Titles(ObservableCollection<AudioModel> songs) =>
        songs.Select(x => x.Title ?? string.Empty).ToArray();
}
