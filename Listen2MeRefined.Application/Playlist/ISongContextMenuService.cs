namespace Listen2MeRefined.Application.Playlist;

public interface ISongContextMenuService
{
    Task<IReadOnlyList<PlaylistMembershipInfo>> GetContextMenuPlaylistsAsync(
        IReadOnlyList<string> selectedSongPaths,
        int? activePlaylistId,
        CancellationToken ct = default);

    Task TogglePlaylistMembershipAsync(
        int playlistId,
        IReadOnlyList<string> selectedSongPaths,
        bool shouldContain,
        bool allowRemove,
        CancellationToken ct = default);

    Task AddToNewPlaylistAsync(
        string playlistName,
        IReadOnlyList<string> selectedSongPaths,
        CancellationToken ct = default);
}
