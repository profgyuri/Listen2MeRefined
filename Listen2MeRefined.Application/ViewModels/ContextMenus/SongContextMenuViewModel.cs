using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Core.Enums;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.ContextMenus;

public partial class SongContextMenuViewModel : ViewModelBase
{
    private readonly IPlaylistMembership _playlistMembership;
    private readonly ISongContextSelectionService _songContextSelectionService;
    private ISongContextMenuHost? _host;
    private bool _isMenuOpen;

    [ObservableProperty] private ObservableCollection<SongContextMenuItemViewModel> _playlists = [];

    public bool ShowPlaylistActions { get; private set; }
    public bool ShowRemoveFromPlaylistAction { get; private set; }
    public bool ShowAddToDefaultPlaylistAction { get; private set; }
    public bool ArePlaybackActionsEnabled { get; private set; }

    public SongContextMenuViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IPlaylistMembership playlistMembership,
        ISongContextSelectionService songContextSelectionService) : base(errorHandler, logger, messenger)
    {
        _playlistMembership = playlistMembership;
        _songContextSelectionService = songContextSelectionService;
    }

    public override Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RegisterMessage<SongContextMenuSelectionChangedMessage>(OnSelectionChangedMessage);
        return Task.CompletedTask;
    }

    public void SetHost(ISongContextMenuHost host)
    {
        _host = host;
    }

    public async Task HandleOpenedAsync(CancellationToken ct = default)
    {
        _isMenuOpen = true;
        await RefreshAsync(ct);
    }

    public void HandleClosed()
    {
        _isMenuOpen = false;
    }

    public async Task TogglePlaylistMembershipAsync(
        SongContextMenuItemViewModel playlist,
        bool shouldContain,
        CancellationToken ct = default)
    {
        var context = GetContext();
        if (context.SelectedSongPaths.Count == 0)
        {
            return;
        }

        await _playlistMembership.TogglePlaylistMembershipAsync(
            playlist.PlaylistId,
            context.SelectedSongPaths,
            shouldContain,
            playlist.AllowRemove,
            ct);

        await RefreshAsync(ct);
    }

    public async Task AddToNewPlaylistAsync(string name, CancellationToken ct = default)
    {
        var normalizedName = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return;
        }

        var context = GetContext();
        if (context.SelectedSongPaths.Count == 0)
        {
            return;
        }

        await _playlistMembership.AddToNewPlaylistAsync(normalizedName, context.SelectedSongPaths, ct);
        await RefreshAsync(ct);
    }

    public Task RescanAsync(CancellationToken ct = default)
    {
        return SendPlaylistActionRequest(PlaylistContextMenuAction.Rescan);
    }

    public Task PlayNowAsync(CancellationToken ct = default)
    {
        return SendPlaylistActionRequest(PlaylistContextMenuAction.PlayNow);
    }

    public Task PlayAfterCurrentAsync(CancellationToken ct = default)
    {
        return SendPlaylistActionRequest(PlaylistContextMenuAction.PlayAfterCurrent);
    }

    public Task RemoveFromPlaylistAsync(CancellationToken ct = default)
    {
        return SendPlaylistActionRequest(
            PlaylistContextMenuAction.RemoveFromPlaylist,
            requireDefaultPlaylistHost: true);
    }

    public Task AddToDefaultPlaylistAsync(CancellationToken ct = default)
    {
        return SendPlaylistActionRequest(PlaylistContextMenuAction.AddToDefaultPlaylist);
    }

    private async Task RefreshAsync(CancellationToken ct = default)
    {
        var context = GetContext();
        ShowPlaylistActions = context.ShowPlaylistMembershipActions;
        ShowRemoveFromPlaylistAction = context.ShowRemoveFromPlaylistAction;
        ShowAddToDefaultPlaylistAction = context.ShowAddToDefaultPlaylistAction;
        ArePlaybackActionsEnabled = _host?.ArePlaybackActionsAvailable ?? false;

        if (context.SelectedSongPaths.Count == 0)
        {
            Playlists = [];
            return;
        }

        var playlistStates = await _playlistMembership.GetPlaylistMembershipInfoAsync(
            context.SelectedSongPaths,
            context.ActivePlaylistId,
            ct);

        var allowRemoveForAny = context.SelectedSongPaths.Count == 1;
        Playlists = new ObservableCollection<SongContextMenuItemViewModel>(
            playlistStates.Select(x => new SongContextMenuItemViewModel(
                x.PlaylistId,
                x.PlaylistName,
                x.ContainsSong,
                allowRemoveForAny || context.ActivePlaylistId == x.PlaylistId)));
    }

    private SongContextSelectionContext GetContext()
    {
        if (_host is null)
        {
            return new SongContextSelectionContext([], null, false, false, false);
        }

        var paths = _songContextSelectionService.ResolveSelectionPaths(
            _host.GetDirectSongContextSelection(),
            _host.GetFallbackSongContextSelection(),
            _host.GetFocusedSong());

        return new SongContextSelectionContext(
            paths,
            _host.GetSongContextActivePlaylistId(),
            _host.ShowPlaylistMembershipActions,
            _host.ShowRemoveFromPlaylistAction,
            _host.ShowAddToDefaultPlaylistAction);
    }

    private void OnSelectionChangedMessage(SongContextMenuSelectionChangedMessage message)
    {
        if (!_isMenuOpen || !ReferenceEquals(message.Value, _host))
        {
            return;
        }

        _ = ExecuteSafeAsync(ct => RefreshAsync(ct));
    }

    private sealed record SongContextSelectionContext(
        IReadOnlyList<string> SelectedSongPaths,
        int? ActivePlaylistId,
        bool ShowPlaylistMembershipActions,
        bool ShowRemoveFromPlaylistAction,
        bool ShowAddToDefaultPlaylistAction);

    private Task SendPlaylistActionRequest(
        PlaylistContextMenuAction action,
        bool requireDefaultPlaylistHost = false)
    {
        if (_host is null)
        {
            return Task.CompletedTask;
        }

        if (requireDefaultPlaylistHost && !_host.ShowRemoveFromPlaylistAction)
        {
            return Task.CompletedTask;
        }

        if (action == PlaylistContextMenuAction.RemoveFromPlaylist && !_host.ShowRemoveFromPlaylistAction)
        {
            return Task.CompletedTask;
        }

        Messenger.Send(new PlaylistContextMenuActionRequestedMessage(
            new PlaylistContextMenuActionRequest(_host, action)));

        return Task.CompletedTask;
    }
}
