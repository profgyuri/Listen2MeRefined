using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;

namespace Listen2MeRefined.Tests.Media;

public class PlaybackQueueServiceTests
{
    [Fact]
    public void ShufflePreservingCurrent_MovesCurrentTrackToStart()
    {
        var tracks = CreateTracks(3);
        var playlist = CreatePlaylist(tracks);
        playlist.CurrentIndex = 2;

        var service = new PlaybackQueueService(playlist);

        var current = service.GetCurrentTrack();
        var shuffledCurrent = service.Shuffle(current);

        Assert.NotNull(shuffledCurrent);
        Assert.Same(shuffledCurrent, playlist[0]);
        Assert.Equal(0, playlist.CurrentIndex);
        DeleteTracks(tracks);
    }

    [Fact]
    public void GetNextTrack_WrapsAroundPlaylist()
    {
        var tracks = CreateTracks(2);
        var playlist = CreatePlaylist(tracks);
        playlist.CurrentIndex = 1;

        var service = new PlaybackQueueService(playlist);

        var next = service.GetNextTrack();

        Assert.Same(tracks[0], next);
        Assert.Equal(0, playlist.CurrentIndex);
        DeleteTracks(tracks);
    }

    [Fact]
    public void GetPreviousTrack_WrapsAroundPlaylist()
    {
        var tracks = CreateTracks(2);
        var playlist = CreatePlaylist(tracks);
        playlist.CurrentIndex = 0;

        var service = new PlaybackQueueService(playlist);

        var previous = service.GetPreviousTrack();

        Assert.Same(tracks[1], previous);
        Assert.Equal(1, playlist.CurrentIndex);
        DeleteTracks(tracks);
    }

    [Fact]
    public void GetCurrentTrack_RemovesInvalidEntriesAndKeepsCurrentPointerSafe()
    {
        var validTrack = CreateTrackWithTempFile();
        var invalidTrack = new AudioModel { Path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid()}.mp3") };

        var playlist = CreatePlaylist([invalidTrack, validTrack]);
        playlist.CurrentIndex = 1;
        var service = new PlaybackQueueService(playlist);

        var current = service.GetCurrentTrack();
        var next = service.GetNextTrack();

        Assert.Null(next);
        Assert.Single(playlist.Items);
        Assert.Same(validTrack, current);
        Assert.Equal(0, playlist.CurrentIndex);

        DeleteTracks([validTrack]);
    }

    private static Playlist CreatePlaylist(IReadOnlyList<AudioModel> tracks)
    {
        var playlist = new Playlist();
        foreach (var track in tracks)
        {
            playlist.Items.Add(track);
        }

        return playlist;
    }

    private static List<AudioModel> CreateTracks(int count)
    {
        var tracks = new List<AudioModel>();
        for (var i = 0; i < count; i++)
        {
            tracks.Add(CreateTrackWithTempFile());
        }

        return tracks;
    }

    private static AudioModel CreateTrackWithTempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"queue-track-{Guid.NewGuid()}.mp3");
        File.WriteAllText(path, "x");
        return new AudioModel { Path = path };
    }

    private static void DeleteTracks(IEnumerable<AudioModel> tracks)
    {
        foreach (var track in tracks)
        {
            if (File.Exists(track.Path))
            {
                File.Delete(track.Path);
            }
        }
    }
}