using System.Text;
using Listen2MeRefined.Application.Playlist.Formats;

namespace Listen2MeRefined.Infrastructure.Playlist.Formats;

/// <summary>
/// Default <see cref="IPlaylistFormatRegistry"/> implementation that resolves formats by extension.
/// </summary>
public sealed class PlaylistFormatRegistry : IPlaylistFormatRegistry
{
    private readonly IReadOnlyList<IPlaylistFileFormat> _formats;

    public PlaylistFormatRegistry(IEnumerable<IPlaylistFileFormat> formats)
    {
        _formats = formats.ToList();
    }

    public IReadOnlyList<IPlaylistFileFormat> Formats => _formats;

    public IPlaylistFileFormat? ResolveForPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var extension = Path.GetExtension(path);
        if (string.IsNullOrEmpty(extension))
        {
            return null;
        }

        foreach (var format in _formats)
        {
            if (format.Extensions.Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase)))
            {
                return format;
            }
        }

        return null;
    }

    public string BuildOpenFilter()
    {
        var builder = new StringBuilder();

        var allPatterns = _formats
            .SelectMany(f => f.Extensions)
            .Select(ToPattern);
        builder.Append("All playlists|");
        builder.Append(string.Join(';', allPatterns));

        foreach (var format in _formats)
        {
            builder.Append('|');
            AppendFormatEntry(builder, format);
        }

        builder.Append("|All files|*.*");
        return builder.ToString();
    }

    public string BuildSaveFilter()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < _formats.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('|');
            }
            AppendFormatEntry(builder, _formats[i]);
        }

        return builder.ToString();
    }

    private static void AppendFormatEntry(StringBuilder builder, IPlaylistFileFormat format)
    {
        var patterns = string.Join(';', format.Extensions.Select(ToPattern));
        builder.Append(format.DisplayName);
        if (!string.IsNullOrWhiteSpace(format.RecommendedUseCase))
        {
            builder.Append(" \u2014 ");
            builder.Append(format.RecommendedUseCase);
        }
        builder.Append('|');
        builder.Append(patterns);
    }

    private static string ToPattern(string extension)
    {
        return extension.StartsWith('.')
            ? "*" + extension.ToLowerInvariant()
            : "*." + extension.ToLowerInvariant();
    }
}
