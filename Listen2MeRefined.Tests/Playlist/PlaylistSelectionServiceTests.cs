using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Playlist;

namespace Listen2MeRefined.Tests.Playlist;

public class PlaylistSelectionServiceTests
{
    [Fact]
    public void ResolveSelectedSongs_WithTabSelection_ReturnsFilteredSelectedSongs()
    {
        var service = new PlaylistSelectionService();
        var selected = new AudioModel { Path = "a.mp3", Title = "A" };
        var selectedOutsideTab = new AudioModel { Path = "x.mp3", Title = "X" };
        var tabSong = new AudioModel { Path = "a.mp3", Title = "A" };
        var otherTabSong = new AudioModel { Path = "b.mp3", Title = "B" };

        var result = service.ResolveSelectedSongs(
            [selected, selectedOutsideTab],
            [tabSong, otherTabSong],
            selectedSong: null);

        Assert.Single(result);
        Assert.Equal("a.mp3", result[0].Path);
    }

    [Fact]
    public void ResolveSelectedSongs_WithNoTabSelection_FallsBackToFocusedSong()
    {
        var service = new PlaylistSelectionService();
        var focused = new AudioModel { Path = "a.mp3", Title = "A" };

        var result = service.ResolveSelectedSongs(
            [],
            [focused],
            focused);

        Assert.Single(result);
        Assert.Same(focused, result[0]);
    }
}
