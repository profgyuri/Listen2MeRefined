using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Listen2MeRefined.Application.Playlist.Formats;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Playlist.Formats;

/// <summary>
/// Reads and writes a richer JSON playlist format that preserves full metadata.
/// </summary>
public sealed class JsonPlaylistFormat : IPlaylistFileFormat
{
    private static readonly IReadOnlyList<string> ExtensionsList = [".json"];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string DisplayName => "JSON";

    public string RecommendedUseCase => "richest metadata, Listen2Me-only";

    public IReadOnlyList<string> Extensions => ExtensionsList;

    public async Task<IReadOnlyList<PlaylistFileEntry>> ReadAsync(
        Stream stream,
        string? sourcePath,
        CancellationToken ct = default)
    {
        try
        {
            var doc = await JsonSerializer.DeserializeAsync<JsonPlaylistDocument>(stream, SerializerOptions, ct);
            if (doc?.Tracks is null)
            {
                return Array.Empty<PlaylistFileEntry>();
            }

            var baseDir = !string.IsNullOrWhiteSpace(sourcePath)
                ? Path.GetDirectoryName(sourcePath)
                : null;

            return doc.Tracks
                .Where(t => !string.IsNullOrWhiteSpace(t.Path))
                .Select(t => new PlaylistFileEntry(
                    ResolvePath(t.Path!, baseDir),
                    t.Title,
                    t.Artist,
                    t.DurationSeconds.HasValue
                        ? TimeSpan.FromSeconds(t.DurationSeconds.Value)
                        : null))
                .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<PlaylistFileEntry>();
        }
    }

    public async Task WriteAsync(
        Stream stream,
        IEnumerable<AudioModel> songs,
        CancellationToken ct = default)
    {
        var doc = new JsonPlaylistDocument
        {
            Tracks = songs
                .Where(s => !string.IsNullOrWhiteSpace(s.Path))
                .Select(s => new JsonTrack
                {
                    Path = s.Path,
                    Title = s.Title,
                    Artist = s.Artist,
                    Genre = s.Genre,
                    DurationSeconds = s.Length.TotalSeconds > 0
                        ? (long)Math.Round(s.Length.TotalSeconds)
                        : null,
                })
                .ToList(),
        };

        await JsonSerializer.SerializeAsync(stream, doc, SerializerOptions, ct);
        await stream.FlushAsync(ct);
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

    private sealed class JsonPlaylistDocument
    {
        public string? Name { get; set; }
        public List<JsonTrack>? Tracks { get; set; }
    }

    private sealed class JsonTrack
    {
        public string? Path { get; set; }
        public string? Title { get; set; }
        public string? Artist { get; set; }
        public string? Genre { get; set; }

        [JsonPropertyName("durationSeconds")]
        public long? DurationSeconds { get; set; }
    }
}
