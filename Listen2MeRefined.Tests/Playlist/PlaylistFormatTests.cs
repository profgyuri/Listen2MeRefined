using System.Text;
using Listen2MeRefined.Application.Playlist.Formats;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Playlist.Formats;

namespace Listen2MeRefined.Tests.Playlist;

public class PlaylistFormatTests
{
    [Fact]
    public async Task M3u8_RoundTrip_PreservesPathAndMetadata()
    {
        var format = new M3u8PlaylistFormat();
        var songs = new[]
        {
            new AudioModel { Path = @"C:\Music\a.mp3", Artist = "A", Title = "X", Length = TimeSpan.FromSeconds(120) },
            new AudioModel { Path = @"C:\Music\b.mp3", Artist = "B", Title = "Y", Length = TimeSpan.FromSeconds(60) },
        };

        await using var writeStream = new MemoryStream();
        await format.WriteAsync(writeStream, songs);

        writeStream.Position = 0;
        var entries = await format.ReadAsync(writeStream, sourcePath: null);

        Assert.Equal(2, entries.Count);
        Assert.Equal(@"C:\Music\a.mp3", entries[0].Path);
        Assert.Equal("A", entries[0].Artist);
        Assert.Equal("X", entries[0].Title);
        Assert.Equal(TimeSpan.FromSeconds(120), entries[0].Duration);
        Assert.Equal("B", entries[1].Artist);
    }

    [Fact]
    public async Task M3u8_Read_ResolvesRelativePathsAgainstSourceDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "l2m-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var sourcePath = Path.Combine(tempDir, "play.m3u");
            var content = "#EXTM3U\n#EXTINF:10,A - X\nnested\\song.mp3\n";
            await File.WriteAllTextAsync(sourcePath, content, new UTF8Encoding(false));

            var format = new M3u8PlaylistFormat();
            await using var stream = File.OpenRead(sourcePath);
            var entries = await format.ReadAsync(stream, sourcePath);

            Assert.Single(entries);
            Assert.Equal(Path.GetFullPath(Path.Combine(tempDir, "nested", "song.mp3")), entries[0].Path);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task M3u8_Read_IgnoresCommentLinesWithoutExtInf()
    {
        var content = "#EXTM3U\n#COMMENT: hello\n#EXTINF:5,Artist - Title\nC:\\song.mp3\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var format = new M3u8PlaylistFormat();
        var entries = await format.ReadAsync(stream, sourcePath: null);

        Assert.Single(entries);
        Assert.Equal("Artist", entries[0].Artist);
        Assert.Equal("Title", entries[0].Title);
    }

    [Fact]
    public async Task M3u8_Read_HandlesBom()
    {
        var content = "#EXTM3U\nC:\\a.mp3\n";
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(content))
            .ToArray();
        await using var stream = new MemoryStream(bytes);

        var format = new M3u8PlaylistFormat();
        var entries = await format.ReadAsync(stream, sourcePath: null);

        Assert.Single(entries);
        Assert.Equal(@"C:\a.mp3", entries[0].Path);
    }

    [Fact]
    public async Task Pls_RoundTrip_PreservesOrderAndLength()
    {
        var format = new PlsPlaylistFormat();
        var songs = new[]
        {
            new AudioModel { Path = @"C:\a.mp3", Artist = "A", Title = "X", Length = TimeSpan.FromSeconds(30) },
            new AudioModel { Path = @"C:\b.mp3", Artist = "B", Title = "Y", Length = TimeSpan.FromSeconds(45) },
        };

        await using var stream = new MemoryStream();
        await format.WriteAsync(stream, songs);

        stream.Position = 0;
        var entries = await format.ReadAsync(stream, sourcePath: null);

        Assert.Equal(2, entries.Count);
        Assert.Equal(@"C:\a.mp3", entries[0].Path);
        Assert.Equal("A - X", entries[0].Title);
        Assert.Equal(TimeSpan.FromSeconds(30), entries[0].Duration);
        Assert.Equal(@"C:\b.mp3", entries[1].Path);
    }

    [Fact]
    public async Task Pls_Read_HandlesIndexGaps()
    {
        var content = """
            [playlist]
            File3=C:\third.mp3
            Title3=Third
            Length3=60
            File1=C:\first.mp3
            Title1=First
            Length1=30
            Version=2
            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var format = new PlsPlaylistFormat();
        var entries = await format.ReadAsync(stream, sourcePath: null);

        Assert.Equal(2, entries.Count);
        Assert.Equal(@"C:\first.mp3", entries[0].Path);
        Assert.Equal(@"C:\third.mp3", entries[1].Path);
    }

    [Fact]
    public async Task Json_RoundTrip_PreservesAllFields()
    {
        var format = new JsonPlaylistFormat();
        var songs = new[]
        {
            new AudioModel { Path = @"C:\a.mp3", Artist = "A", Title = "X", Genre = "Rock", Length = TimeSpan.FromSeconds(180) },
        };

        await using var stream = new MemoryStream();
        await format.WriteAsync(stream, songs);

        stream.Position = 0;
        var entries = await format.ReadAsync(stream, sourcePath: null);

        Assert.Single(entries);
        Assert.Equal(@"C:\a.mp3", entries[0].Path);
        Assert.Equal("A", entries[0].Artist);
        Assert.Equal("X", entries[0].Title);
        Assert.Equal(TimeSpan.FromSeconds(180), entries[0].Duration);
    }

    [Fact]
    public async Task Json_MalformedInput_ReturnsEmpty()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{not-json"));
        var format = new JsonPlaylistFormat();

        var entries = await format.ReadAsync(stream, sourcePath: null);

        Assert.Empty(entries);
    }

    [Fact]
    public void Registry_ResolveForPath_MatchesByExtensionIgnoringCase()
    {
        var registry = new PlaylistFormatRegistry(new IPlaylistFileFormat[]
        {
            new M3u8PlaylistFormat(),
            new PlsPlaylistFormat(),
            new JsonPlaylistFormat(),
        });

        Assert.IsType<M3u8PlaylistFormat>(registry.ResolveForPath(@"C:\x.M3U"));
        Assert.IsType<PlsPlaylistFormat>(registry.ResolveForPath(@"C:\x.PLS"));
        Assert.IsType<JsonPlaylistFormat>(registry.ResolveForPath(@"C:\x.json"));
        Assert.Null(registry.ResolveForPath(@"C:\x.mp3"));
    }

    [Fact]
    public void Registry_BuildOpenFilter_IncludesAggregateAndPerFormat()
    {
        var registry = new PlaylistFormatRegistry(new IPlaylistFileFormat[]
        {
            new M3u8PlaylistFormat(),
            new JsonPlaylistFormat(),
        });

        var filter = registry.BuildOpenFilter();

        Assert.Contains("All playlists", filter);
        Assert.Contains("*.m3u", filter);
        Assert.Contains("*.json", filter);
        Assert.Contains("M3U / M3U8", filter);
        Assert.Contains("All files|*.*", filter);
    }
}
