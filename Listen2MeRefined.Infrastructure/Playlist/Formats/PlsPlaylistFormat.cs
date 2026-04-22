using System.Globalization;
using System.Text;
using Listen2MeRefined.Application.Playlist.Formats;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Playlist.Formats;

/// <summary>
/// Reads and writes PLS (Winamp-era) playlists.
/// </summary>
public sealed class PlsPlaylistFormat : IPlaylistFileFormat
{
    private static readonly IReadOnlyList<string> ExtensionsList = [".pls"];

    public string DisplayName => "PLS";

    public string RecommendedUseCase => "legacy Winamp-era format";

    public IReadOnlyList<string> Extensions => ExtensionsList;

    public async Task<IReadOnlyList<PlaylistFileEntry>> ReadAsync(
        Stream stream,
        string? sourcePath,
        CancellationToken ct = default)
    {
        var rows = new SortedDictionary<int, Row>();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = (await reader.ReadLineAsync(ct))?.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('[') || !line.Contains('='))
            {
                continue;
            }

            var equalsIndex = line.IndexOf('=');
            var key = line[..equalsIndex].Trim();
            var value = line[(equalsIndex + 1)..].Trim();

            if (TryParseKey(key, "File", out var index))
            {
                GetOrAdd(rows, index).Path = value;
            }
            else if (TryParseKey(key, "Title", out index))
            {
                GetOrAdd(rows, index).Title = value;
            }
            else if (TryParseKey(key, "Length", out index))
            {
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds > 0)
                {
                    GetOrAdd(rows, index).Duration = TimeSpan.FromSeconds(seconds);
                }
            }
        }

        var baseDir = !string.IsNullOrWhiteSpace(sourcePath)
            ? Path.GetDirectoryName(sourcePath)
            : null;

        return rows.Values
            .Where(r => !string.IsNullOrWhiteSpace(r.Path))
            .Select(r => new PlaylistFileEntry(
                ResolvePath(r.Path!, baseDir),
                r.Title,
                Artist: null,
                r.Duration))
            .ToList();
    }

    public async Task WriteAsync(
        Stream stream,
        IEnumerable<AudioModel> songs,
        CancellationToken ct = default)
    {
        var items = songs.Where(s => !string.IsNullOrWhiteSpace(s.Path)).ToList();

        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
        await writer.WriteLineAsync("[playlist]");
        await writer.WriteLineAsync($"NumberOfEntries={items.Count.ToString(CultureInfo.InvariantCulture)}");

        for (var i = 0; i < items.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var song = items[i];
            var index = i + 1;
            var title = string.IsNullOrWhiteSpace(song.Artist)
                ? song.Title ?? string.Empty
                : $"{song.Artist} - {song.Title}";
            var seconds = song.Length.TotalSeconds > 0
                ? (long)Math.Round(song.Length.TotalSeconds)
                : -1;

            await writer.WriteLineAsync($"File{index}={song.Path}");
            await writer.WriteLineAsync($"Title{index}={title}");
            await writer.WriteLineAsync($"Length{index}={seconds.ToString(CultureInfo.InvariantCulture)}");
        }

        await writer.WriteLineAsync("Version=2");
        await writer.FlushAsync(ct);
    }

    private static bool TryParseKey(string key, string prefix, out int index)
    {
        index = 0;
        if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var tail = key[prefix.Length..];
        return int.TryParse(tail, NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }

    private static Row GetOrAdd(SortedDictionary<int, Row> rows, int index)
    {
        if (!rows.TryGetValue(index, out var row))
        {
            row = new Row();
            rows[index] = row;
        }

        return row;
    }

    private static string ResolvePath(string line, string? baseDir)
    {
        if (string.IsNullOrWhiteSpace(baseDir) || Path.IsPathRooted(line))
        {
            return line;
        }

        try
        {
            return Path.GetFullPath(Path.Combine(baseDir, line));
        }
        catch
        {
            return line;
        }
    }

    private sealed class Row
    {
        public string? Path { get; set; }
        public string? Title { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}
