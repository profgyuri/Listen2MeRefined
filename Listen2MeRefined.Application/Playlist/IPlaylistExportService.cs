using Listen2MeRefined.Application.Playlist.Formats;

namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Describes the playlist to export: <see langword="null"/> <paramref name="PlaylistId"/> means the default playlist.
/// </summary>
/// <param name="PlaylistId">The named playlist identifier, or <see langword="null"/> for the default playlist.</param>
/// <param name="DisplayName">User-facing name used for status messages.</param>
public sealed record PlaylistExportSource(int? PlaylistId, string DisplayName);

/// <summary>
/// Exports a playlist to a playlist file in the requested format.
/// </summary>
public interface IPlaylistExportService
{
    /// <summary>
    /// Writes the contents of <paramref name="source"/> to <paramref name="targetPath"/> using <paramref name="format"/>.
    /// </summary>
    Task ExportAsync(PlaylistExportSource source, string targetPath, IPlaylistFileFormat format, CancellationToken ct = default);
}
