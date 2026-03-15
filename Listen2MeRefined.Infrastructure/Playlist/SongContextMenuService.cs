using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Playlist;

namespace Listen2MeRefined.Infrastructure.Playlist;

public sealed class SongContextMenuService : ISongContextMenuService
{
    private readonly IPlaylistLibraryService _playlistLibraryService;
    private readonly IMediator _mediator;
    private readonly IMessenger _messenger;

    public SongContextMenuService(
        IPlaylistLibraryService playlistLibraryService,
        IMediator mediator,
        IMessenger messenger)
    {
        _playlistLibraryService = playlistLibraryService;
        _mediator = mediator;
        _messenger = messenger;
    }

    public async Task<IReadOnlyList<PlaylistMembershipInfo>> GetContextMenuPlaylistsAsync(
        IReadOnlyList<string> selectedSongPaths,
        int? activePlaylistId,
        CancellationToken ct = default)
    {
        var normalizedPaths = NormalizePaths(selectedSongPaths);
        if (normalizedPaths.Count == 0)
        {
            return [];
        }

        if (normalizedPaths.Count == 1)
        {
            return await _playlistLibraryService.GetMembershipBySongPathAsync(normalizedPaths[0], ct);
        }

        var allPlaylists = await _playlistLibraryService.GetAllPlaylistsAsync(ct);
        return allPlaylists
            .Select(x => new PlaylistMembershipInfo(x.Id, x.Name, activePlaylistId == x.Id))
            .ToArray();
    }

    public async Task TogglePlaylistMembershipAsync(
        int playlistId,
        IReadOnlyList<string> selectedSongPaths,
        bool shouldContain,
        bool allowRemove,
        CancellationToken ct = default)
    {
        var normalizedPaths = NormalizePaths(selectedSongPaths);
        if (normalizedPaths.Count == 0)
        {
            return;
        }

        if (shouldContain)
        {
            await _playlistLibraryService.AddSongsByPathAsync(playlistId, normalizedPaths, ct);
            await _mediator.Publish(new PlaylistMembershipChangedNotification(playlistId), ct);
            _messenger.Send(new PlaylistMembershipChangedMessage(new PlaylistMembershipChangedMessageData(playlistId)));
            return;
        }

        if (!allowRemove)
        {
            return;
        }

        await _playlistLibraryService.RemoveSongsByPathAsync(playlistId, normalizedPaths, ct);
        await _mediator.Publish(new PlaylistMembershipChangedNotification(playlistId), ct);
        _messenger.Send(new PlaylistMembershipChangedMessage(new PlaylistMembershipChangedMessageData(playlistId)));
    }

    public async Task AddToNewPlaylistAsync(
        string playlistName,
        IReadOnlyList<string> selectedSongPaths,
        CancellationToken ct = default)
    {
        var normalizedName = playlistName?.Trim() ?? string.Empty;
        var normalizedPaths = NormalizePaths(selectedSongPaths);
        if (string.IsNullOrWhiteSpace(normalizedName) || normalizedPaths.Count == 0)
        {
            return;
        }

        var created = await _playlistLibraryService.CreatePlaylistAsync(normalizedName, ct);
        await _playlistLibraryService.AddSongsByPathAsync(created.Id, normalizedPaths, ct);

        await _mediator.Publish(new PlaylistCreatedNotification(created.Id, created.Name), ct);
        await _mediator.Publish(new PlaylistMembershipChangedNotification(created.Id), ct);
        _messenger.Send(new PlaylistCreatedMessage(new PlaylistCreatedMessageData(created.Id, created.Name)));
        _messenger.Send(new PlaylistMembershipChangedMessage(new PlaylistMembershipChangedMessageData(created.Id)));
    }

    private static IReadOnlyList<string> NormalizePaths(IEnumerable<string> selectedSongPaths)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in selectedSongPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            normalized.Add(path.Trim());
        }

        return normalized.ToArray();
    }
}
