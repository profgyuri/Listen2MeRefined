namespace Listen2MeRefined.Application.Playlist.Formats;

/// <summary>
/// Central lookup for playlist file formats, used to resolve readers/writers and build dialog filters.
/// </summary>
public interface IPlaylistFormatRegistry
{
    /// <summary>Gets all registered formats.</summary>
    IReadOnlyList<IPlaylistFileFormat> Formats { get; }

    /// <summary>
    /// Returns the format matching the extension of <paramref name="path"/>, or <see langword="null"/> if none.
    /// </summary>
    IPlaylistFileFormat? ResolveForPath(string path);

    /// <summary>
    /// Builds a Win32-style filter string for open dialogs that includes an "All playlists" aggregate,
    /// one entry per registered format (with recommendation hint), and a trailing "All files" wildcard.
    /// </summary>
    string BuildOpenFilter();

    /// <summary>
    /// Builds a Win32-style filter string for save dialogs that lists one entry per registered format
    /// (with recommendation hint).
    /// </summary>
    string BuildSaveFilter();
}
