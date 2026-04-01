using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.Enums;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.ContextMenus;

public partial class SongContextMenuViewModel : ViewModelBase
{
    private readonly IPlaylistMembership _playlistMembership;
    private readonly ISongContextSelectionService _songContextSelectionService;
    private ViewModelBase? _hostViewModel;
    private bool _isMenuOpen;

    [ObservableProperty] private ObservableCollection<SongContextMenuItemViewModel> _playlists = [];

    public bool ShowPlaylistActions { get; private set; }
    public bool ShowRemoveFromPlaylistAction { get; private set; }

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

    public void SetHost(ViewModelBase hostViewModel)
    {
        _hostViewModel = hostViewModel;
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

    private async Task RefreshAsync(CancellationToken ct = default)
    {
        var context = GetContext();
        ShowPlaylistActions = context.IsPlaylistHost;
        ShowRemoveFromPlaylistAction = context.IsDefaultPlaylistHost;

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
        if (_hostViewModel is SearchResultsPaneViewModel searchResultsPaneViewModel)
        {
            return new SongContextSelectionContext(
                _songContextSelectionService.ResolveSearchSelectionPaths(
                    searchResultsPaneViewModel.GetDirectSongContextSelection(),
                    searchResultsPaneViewModel.GetFallbackSongContextSelection()),
                searchResultsPaneViewModel.GetSongContextActivePlaylistId(),
                IsPlaylistHost: false,
                IsDefaultPlaylistHost: false);
        }

        if (_hostViewModel is PlaylistPaneViewModel playlistPaneViewModel)
        {
            return new SongContextSelectionContext(
                _songContextSelectionService.ResolvePlaylistSelectionPaths(
                    playlistPaneViewModel.GetSelectedTabSongContextSelection(),
                    playlistPaneViewModel.GetCurrentTabSongContextSelection(),
                    playlistPaneViewModel.SelectedSong),
                playlistPaneViewModel.GetSongContextActivePlaylistId(),
                IsPlaylistHost: true,
                IsDefaultPlaylistHost: playlistPaneViewModel.IsDefaultPlaylist);
        }

        return new SongContextSelectionContext([], null, IsPlaylistHost: false, IsDefaultPlaylistHost: false);
    }

    private void OnSelectionChangedMessage(SongContextMenuSelectionChangedMessage message)
    {
        if (!_isMenuOpen || !ReferenceEquals(message.Value, _hostViewModel))
        {
            return;
        }

        _ = ExecuteSafeAsync(ct => RefreshAsync(ct));
    }

    private sealed record SongContextSelectionContext(
        IReadOnlyList<string> SelectedSongPaths,
        int? ActivePlaylistId,
        bool IsPlaylistHost,
        bool IsDefaultPlaylistHost);

    private Task SendPlaylistActionRequest(
        PlaylistContextMenuAction action,
        bool requireDefaultPlaylistHost = false)
    {
        if (_hostViewModel is not PlaylistPaneViewModel playlistHost)
        {
            return Task.CompletedTask;
        }

        if (requireDefaultPlaylistHost && !playlistHost.IsDefaultPlaylist)
        {
            return Task.CompletedTask;
        }

        Messenger.Send(new PlaylistContextMenuActionRequestedMessage(
            new PlaylistContextMenuActionRequest(playlistHost, action)));

        return Task.CompletedTask;
    }
}
