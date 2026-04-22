namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Imports an M3U/PLS/JSON playlist file into the default playlist.
/// </summary>
public interface IPlaylistImportService
{
    /// <summary>
    /// Parses the playlist at <paramref name="playlistFilePath"/>, resolves each track,
    /// prompts the user to confirm replacement when the default playlist is non-empty,
    /// and then replaces the default playlist with the resolved tracks. Missing files
    /// are skipped and reported via the background-task status service.
    /// </summary>
    Task ImportAsync(string playlistFilePath, CancellationToken ct = default);
}
