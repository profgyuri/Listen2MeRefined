using System.Globalization;
using System.Text;
using Listen2MeRefined.Application.Playlist.Formats;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Playlist.Formats;

/// <summary>
/// Reads and writes M3U / M3U8 playlists. Written output is UTF-8 with an #EXTM3U header.
/// </summary>
public sealed class M3u8PlaylistFormat : IPlaylistFileFormat
{
    private static readonly IReadOnlyList<string> ExtensionsList = [".m3u", ".m3u8"];

    public string DisplayName => "M3U / M3U8";

    public string RecommendedUseCase => "recommended for cross-app compatibility (AIMP, VLC, Winamp)";

    public IReadOnlyList<string> Extensions => ExtensionsList;

    public async Task<IReadOnlyList<PlaylistFileEntry>> ReadAsync(
        Stream stream,
        string? sourcePath,
        CancellationToken ct = default)
    {
        var entries = new List<PlaylistFileEntry>();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        string? pendingTitle = null;
        string? pendingArtist = null;
        TimeSpan? pendingDuration = null;

        var baseDir = !string.IsNullOrWhiteSpace(sourcePath)
            ? Path.GetDirectoryName(sourcePath)
            : null;

        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = (await reader.ReadLineAsync(ct))?.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                ParseExtInf(line, out pendingDuration, out pendingArtist, out pendingTitle);
                continue;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            var path = ResolvePath(line, baseDir);
            entries.Add(new PlaylistFileEntry(path, pendingTitle, pendingArtist, pendingDuration));

            pendingTitle = null;
            pendingArtist = null;
            pendingDuration = null;
        }

        return entries;
    }

    public async Task WriteAsync(
        Stream stream,
        IEnumerable<AudioModel> songs,
        CancellationToken ct = default)
    {
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
        await writer.WriteLineAsync("#EXTM3U");

        foreach (var song in songs)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(song.Path))
            {
                continue;
            }

            var seconds = song.Length.TotalSeconds > 0
                ? (long)Math.Round(song.Length.TotalSeconds)
                : -1;
            var titlePart = string.IsNullOrWhiteSpace(song.Artist)
                ? song.Title ?? string.Empty
                : $"{song.Artist} - {song.Title}";

            await writer.WriteLineAsync($"#EXTINF:{seconds.ToString(CultureInfo.InvariantCulture)},{titlePart}");
            await writer.WriteLineAsync(song.Path);
        }

        await writer.FlushAsync(ct);
    }

    private static void ParseExtInf(
        string line,
        out TimeSpan? duration,
        out string? artist,
        out string? title)
    {
        duration = null;
        artist = null;
        title = null;

        var payload = line.Substring("#EXTINF:".Length);
        var commaIndex = payload.IndexOf(',');
        if (commaIndex < 0)
        {
            return;
        }

        var durationText = payload[..commaIndex].Trim();
        if (long.TryParse(durationText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds > 0)
        {
            duration = TimeSpan.FromSeconds(seconds);
        }

        var titleBlock = payload[(commaIndex + 1)..].Trim();
        if (titleBlock.Length == 0)
        {
            return;
        }

        var separatorIndex = titleBlock.IndexOf(" - ", StringComparison.Ordinal);
        if (separatorIndex > 0)
        {
            artist = titleBlock[..separatorIndex].Trim();
            title = titleBlock[(separatorIndex + 3)..].Trim();
        }
        else
        {
            title = titleBlock;
        }
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
}
