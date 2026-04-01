using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Playlist;

public sealed class PlaylistLibraryService(
    IDbContextFactory<DataContext> dataContextFactory,
    ILogger logger) : IPlaylistLibraryService
{
    private const int MinNameLength = 2;
    private const int MaxNameLength = 50;

    public async Task<IReadOnlyList<PlaylistSummary>> GetAllPlaylistsAsync(CancellationToken ct = default)
    {
        await using var context = await dataContextFactory.CreateDbContextAsync(ct);
        return await context.Playlists
            .AsNoTracking()
            .OrderByDescending(x => x.IsPinned)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new PlaylistSummary(x.Id, x.Name!, x.IsPinned, x.DisplayOrder))
            .ToArrayAsync(ct);
    }

    public async Task<IReadOnlyList<AudioModel>> GetPlaylistSongsAsync(int playlistId, CancellationToken ct = default)
    {
        await using var context = await dataContextFactory.CreateDbContextAsync(ct);
        var playlist = await context.Playlists
            .AsNoTracking()
            .Include(x => x.Songs)
            .FirstOrDefaultAsync(x => x.Id == playlistId, ct);

        if (playlist is null)
        {
            return Array.Empty<AudioModel>();
        }

        return playlist.Songs
            .OrderBy(x => x.Display, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<PlaylistSummary> CreatePlaylistAsync(string name, CancellationToken ct = default)
    {
        var normalizedName = NormalizeAndValidateName(name);

        await using var context = await dataContextFactory.CreateDbContextAsync(ct);
        var exists = await context.Playlists
            .AnyAsync(x => x.Name != null && x.Name.ToLower() == normalizedName.ToLower(), ct);
        if (exists)
        {
            throw new InvalidOperationException($"Playlist '{normalizedName}' already exists.");
        }

        var maxOrder = await context.Playlists.MaxAsync(x => (int?)x.DisplayOrder, ct) ?? 0;

        var playlist = new PlaylistModel
        {
            Name = normalizedName,
            DisplayOrder = maxOrder + 1
        };

        context.Playlists.Add(playlist);
        await context.SaveChangesAsync(ct);

        logger.Information("[PlaylistLibraryService] Created playlist {PlaylistName}", normalizedName);
        return new PlaylistSummary(playlist.Id, normalizedName, false, playlist.DisplayOrder);
    }

    public async Task RenamePlaylistAsync(int playlistId, string newName, CancellationToken ct = default)
    {
        var normalizedName = NormalizeAndValidateName(newName);

        await using var context = await dataContextFactory.CreateDbContextAsync(ct);
        var playlist = await context.Playlists.FirstOrDefaultAsync(x => x.Id == playlistId, ct);
        if (playlist is null)
        {
            throw new InvalidOperationException($"Playlist with id {playlistId} was not found.");
        }

        var exists = await context.Playlists
            .AnyAsync(
                x => x.Id != playlistId
                     && x.Name != null
                     && x.Name.ToLower() == normalizedName.ToLower(),
                ct);
        if (exists)
        {
            throw new InvalidOperationException($"Playlist '{normalizedName}' already exists.");
        }

        playlist.Name = normalizedName;
        await context.SaveChangesAsync(ct);

        logger.Information("[PlaylistLibraryService] Renamed playlist {PlaylistId} to {PlaylistName}", playlistId, normalizedName);
    }

    public async Task DeletePlaylistAsync(int playlistId, CancellationToken ct = default)
    {
        await using var context = await dataContextFactory.CreateDbContextAsync(ct);
        var playlist = await context.Playlists.FirstOrDefaultAsync(x => x.Id == playlistId, ct);
        if (playlist is null)
        {
            return;
        }

        context.Playlists.Remove(playlist);
        await context.SaveChangesAsync(ct);

        logger.Information("[PlaylistLibraryService] Deleted playlist {PlaylistId}", playlistId);
    }

    public async Task AddSongsByPathAsync(int playlistId, IEnumerable<string?> songPaths, CancellationToken ct = default)
    {
        var normalizedPaths = NormalizePaths(songPaths);
        if (normalizedPaths.Count == 0)
        {
            return;
        }

        var lowered = normalizedPaths.Select(x => x.ToLowerInvariant()).ToArray();

        await using var context = await dataContextFactory.CreateDbContextAsync(ct);
        var playlist = await context.Playlists
            .Include(x => x.Songs)
            .FirstOrDefaultAsync(x => x.Id == playlistId, ct);
        if (playlist is null)
        {
            return;
        }

        var songs = await context.Songs
            .Where(x => x.Path != null && lowered.Contains(x.Path.ToLower()))
            .ToListAsync(ct);

        if (songs.Count == 0)
        {
            return;
        }

        var existingPathSet = playlist.Songs
            .Where(x => !string.IsNullOrWhiteSpace(x.Path))
            .Select(x => x.Path!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var changed = false;
        foreach (var song in songs)
        {
            if (string.IsNullOrWhiteSpace(song.Path) || existingPathSet.Contains(song.Path))
            {
                continue;
            }

            playlist.Songs.Add(song);
            existingPathSet.Add(song.Path);
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        await context.SaveChangesAsync(ct);
        logger.Information("[PlaylistLibraryService] Added {Count} song(s) to playlist {PlaylistId}", songs.Count, playlistId);
    }

    public async Task RemoveSongsByPathAsync(int playlistId, IEnumerable<string?> songPaths, CancellationToken ct = default)
    {
        var normalizedPaths = NormalizePaths(songPaths);
        if (normalizedPaths.Count == 0)
        {
            return;
        }

        await using var context = await dataContextFactory.CreateDbContextAsync(ct);
        var playlist = await context.Playlists
            .Include(x => x.Songs)
            .FirstOrDefaultAsync(x => x.Id == playlistId, ct);
        if (playlist is null)
        {
            return;
        }

        var toRemove = playlist.Songs
            .Where(x => !string.IsNullOrWhiteSpace(x.Path) && normalizedPaths.Contains(x.Path!))
            .ToArray();

        if (toRemove.Length == 0)
        {
            return;
        }

        foreach (var song in toRemove)
        {
            playlist.Songs.Remove(song);
        }

        await context.SaveChangesAsync(ct);
        logger.Information("[PlaylistLibraryService] Removed {Count} song(s) from playlist {PlaylistId}", toRemove.Length, playlistId);
    }

    public async Task<IReadOnlyList<PlaylistMembershipInfo>> GetMembershipBySongPathAsync(string songPath, CancellationToken ct = default)
    {
        var normalizedPath = songPath?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return Array.Empty<PlaylistMembershipInfo>();
        }

        var loweredPath = normalizedPath.ToLowerInvariant();

        await using var context = await dataContextFactory.CreateDbContextAsync(ct);
        var allPlaylists = await context.Playlists
            .AsNoTracking()
            .OrderByDescending(x => x.IsPinned)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new PlaylistSummary(x.Id, x.Name!, x.IsPinned, x.DisplayOrder))
            .ToArrayAsync(ct);

        if (allPlaylists.Length == 0)
        {
            return Array.Empty<PlaylistMembershipInfo>();
        }

        var containedIds = await context.Playlists
            .AsNoTracking()
            .Where(x => x.Songs.Any(song => song.Path != null && song.Path.ToLower() == loweredPath))
            .Select(x => x.Id)
            .ToArrayAsync(ct);

        var containedSet = containedIds.ToHashSet();
        return allPlaylists
            .Select(x => new PlaylistMembershipInfo(x.Id, x.Name, containedSet.Contains(x.Id)))
            .ToArray();
    }

    public async Task SetPinnedAsync(int playlistId, bool isPinned, CancellationToken ct = default)
    {
        await using var context = await dataContextFactory.CreateDbContextAsync(ct);
        var playlist = await context.Playlists.FirstOrDefaultAsync(x => x.Id == playlistId, ct);
        if (playlist is null)
        {
            return;
        }

        playlist.IsPinned = isPinned;
        await context.SaveChangesAsync(ct);

        logger.Information("[PlaylistLibraryService] Set playlist {PlaylistId} pinned={IsPinned}", playlistId, isPinned);
    }

    public async Task ReorderPlaylistsAsync(IReadOnlyList<(int PlaylistId, int NewOrder)> ordering, CancellationToken ct = default)
    {
        if (ordering.Count == 0)
        {
            return;
        }

        await using var context = await dataContextFactory.CreateDbContextAsync(ct);
        var ids = ordering.Select(x => x.PlaylistId).ToArray();
        var playlists = await context.Playlists
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(ct);

        var orderMap = ordering.ToDictionary(x => x.PlaylistId, x => x.NewOrder);
        foreach (var playlist in playlists)
        {
            if (orderMap.TryGetValue(playlist.Id, out var newOrder))
            {
                playlist.DisplayOrder = newOrder;
            }
        }

        await context.SaveChangesAsync(ct);
        logger.Information("[PlaylistLibraryService] Reordered {Count} playlists", ordering.Count);
    }

    private static string NormalizeAndValidateName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (normalized.Length < MinNameLength || normalized.Length > MaxNameLength)
        {
            throw new InvalidOperationException($"Playlist name length must be between {MinNameLength} and {MaxNameLength} characters.");
        }

        return normalized;
    }

    private static HashSet<string> NormalizePaths(IEnumerable<string?> songPaths)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in songPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            normalized.Add(path.Trim());
        }

        return normalized;
    }
}
