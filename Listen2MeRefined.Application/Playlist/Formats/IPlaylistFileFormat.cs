using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist.Formats;

/// <summary>
/// A reader/writer for a specific playlist file format.
/// </summary>
public interface IPlaylistFileFormat
{
    /// <summary>Gets a short, stable identifier used in dialog filters (e.g. "M3U / M3U8").</summary>
    string DisplayName { get; }

    /// <summary>Gets a short hint describing when to pick this format.</summary>
    string RecommendedUseCase { get; }

    /// <summary>Gets the file extensions this format handles. Each entry includes the leading dot.</summary>
    IReadOnlyList<string> Extensions { get; }

    /// <summary>
    /// Reads playlist entries from the supplied stream.
    /// </summary>
    /// <param name="stream">The input stream positioned at the start of the playlist file.</param>
    /// <param name="sourcePath">The path of the source file, used to resolve relative paths. May be <see langword="null"/> when reading from an in-memory stream.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    /// <returns>The ordered list of parsed entries.</returns>
    Task<IReadOnlyList<PlaylistFileEntry>> ReadAsync(
        Stream stream,
        string? sourcePath,
        CancellationToken ct = default);

    /// <summary>
    /// Writes the given songs to the supplied stream.
    /// </summary>
    /// <param name="stream">The output stream.</param>
    /// <param name="songs">Songs to serialize.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    Task WriteAsync(
        Stream stream,
        IEnumerable<AudioModel> songs,
        CancellationToken ct = default);
}
