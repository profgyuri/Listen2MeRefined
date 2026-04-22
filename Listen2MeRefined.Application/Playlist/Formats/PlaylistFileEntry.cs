namespace Listen2MeRefined.Application.Playlist.Formats;

/// <summary>
/// A single parsed entry read from a playlist file.
/// </summary>
/// <param name="Path">The file path referenced by the entry.</param>
/// <param name="Title">Optional display title.</param>
/// <param name="Artist">Optional artist name.</param>
/// <param name="Duration">Optional duration.</param>
public sealed record PlaylistFileEntry(
    string Path,
    string? Title,
    string? Artist,
    TimeSpan? Duration);
