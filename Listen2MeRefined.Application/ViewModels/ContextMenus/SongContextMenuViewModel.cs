using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.ContextMenus;

public partial class SongContextMenuViewModel : ViewModelBase
{
    private readonly ISongContextMenuService _songContextMenuService;
    private readonly ISongContextSelectionService _songContextSelectionService;
    private ViewModelBase? _hostViewModel;
    private bool _isMenuOpen;

    [ObservableProperty] private ObservableCollection<SongContextMenuItemViewModel> _playlists = [];

    public SongContextMenuViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        ISongContextMenuService songContextMenuService,
        ISongContextSelectionService songContextSelectionService) : base(errorHandler, logger, messenger)
    {
        _songContextMenuService = songContextMenuService;
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

        await _songContextMenuService.TogglePlaylistMembershipAsync(
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

        await _songContextMenuService.AddToNewPlaylistAsync(normalizedName, context.SelectedSongPaths, ct);
        await RefreshAsync(ct);
    }

    private async Task RefreshAsync(CancellationToken ct = default)
    {
        var context = GetContext();
        if (context.SelectedSongPaths.Count == 0)
        {
            Playlists = [];
            return;
        }

        var playlistStates = await _songContextMenuService.GetContextMenuPlaylistsAsync(
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
                searchResultsPaneViewModel.GetSongContextActivePlaylistId());
        }

        if (_hostViewModel is PlaylistPaneViewModel playlistPaneViewModel)
        {
            return new SongContextSelectionContext(
                _songContextSelectionService.ResolvePlaylistSelectionPaths(
                    playlistPaneViewModel.GetSelectedTabSongContextSelection(),
                    playlistPaneViewModel.GetCurrentTabSongContextSelection(),
                    playlistPaneViewModel.SelectedSong),
                playlistPaneViewModel.GetSongContextActivePlaylistId());
        }

        return new SongContextSelectionContext([], null);
    }

    private void OnSelectionChangedMessage(SongContextMenuSelectionChangedMessage message)
    {
        if (!_isMenuOpen || !ReferenceEquals(message.Value, _hostViewModel))
        {
            return;
        }

        _ = ExecuteSafeAsync(ct => RefreshAsync(ct));
    }

    private sealed record SongContextSelectionContext(IReadOnlyList<string> SelectedSongPaths, int? ActivePlaylistId);
}

public sealed class SongContextMenuItemViewModel(
    int playlistId,
    string playlistName,
    bool isChecked,
    bool allowRemove)
{
    public int PlaylistId { get; } = playlistId;
    public string PlaylistName { get; } = playlistName;
    public bool IsChecked { get; set; } = isChecked;
    public bool AllowRemove { get; } = allowRemove;
}
