using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Playlist;

public interface IPlaylistLibraryService
{
    Task<IReadOnlyList<PlaylistSummary>> GetAllPlaylistsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AudioModel>> GetPlaylistSongsAsync(int playlistId, CancellationToken ct = default);
    Task<PlaylistSummary> CreatePlaylistAsync(string name, CancellationToken ct = default);
    Task RenamePlaylistAsync(int playlistId, string newName, CancellationToken ct = default);
    Task DeletePlaylistAsync(int playlistId, CancellationToken ct = default);
    Task AddSongsByPathAsync(int playlistId, IEnumerable<string?> songPaths, CancellationToken ct = default);
    Task RemoveSongsByPathAsync(int playlistId, IEnumerable<string?> songPaths, CancellationToken ct = default);
    Task<IReadOnlyList<PlaylistMembershipInfo>> GetMembershipBySongPathAsync(string songPath, CancellationToken ct = default);
}
