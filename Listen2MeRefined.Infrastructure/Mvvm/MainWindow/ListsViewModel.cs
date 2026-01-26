namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Mvvm.Utils;

public sealed partial class ListsViewModel :
    ViewModelBase,
    IDisposable,
    INotificationHandler<CurrentSongNotification>,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<AdvancedSearchNotification>,
    INotificationHandler<QuickSearchResultsNotification>
{
    private readonly ILogger _logger;
    private readonly IPlaylistStore _playlistStore;
    private readonly IAdvancedDataReader<ParameterizedQuery, AudioModel> _advancedAudioReader;
    private readonly IFileScanner _fileScanner;
    private readonly IMediaController _mediaController;
    private readonly IUiDispatcher _ui;

    private int _currentSongIndex = -1;
    private readonly HashSet<AudioModel> _selectedSearchResults = new();
    private readonly HashSet<AudioModel> _selectedPlaylistItems = new();

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private AudioModel? _selectedSong;
    [ObservableProperty] private int _selectedIndex = -1;
    [ObservableProperty] private BulkObservableCollection<AudioModel> _searchResults = new();
    [ObservableProperty] private BulkObservableCollection<AudioModel> _playList = new();
    [ObservableProperty] private bool _isSearchResultsTabVisible = true;
    [ObservableProperty] private bool _isSongMenuTabVisible;

    public ListsViewModel(
        ILogger logger,
        IPlaylistStore playlistStore,
        IAdvancedDataReader<ParameterizedQuery, AudioModel> advancedAudioReader,
        IFileScanner fileScanner,
        IMediaController mediaController, 
        IUiDispatcher ui)
    {
        _logger = logger;
        _playlistStore = playlistStore;
        _advancedAudioReader = advancedAudioReader;
        _fileScanner = fileScanner;
        _mediaController = mediaController;
        _ui = ui;
    }

    protected override Task InitializeCoreAsync(CancellationToken ct)
    {
        var snapshot = _playlistStore.Snapshot();
        _ui.InvokeAsync(() => PlayList.ReplaceWith(snapshot.ToList()), ct);

        _playlistStore.Changed += PlaylistStoreOnChanged;

        return Task.CompletedTask;
    }
    
    private void PlaylistStoreOnChanged(object? sender, EventArgs e)
    {
        _logger.Verbose("Playlist store changed");
        _ui.InvokeAsync(() =>
        {
            var snapshot = _playlistStore.Snapshot();
            PlayList.ReplaceWith(snapshot.ToList());
            _logger.Information("Playlist items replaced with {Count} new elements", PlayList.Count);
        });
    }

    #region Notifcation Handlers
    public async Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        await _ui.InvokeAsync(() => FontFamily = notification.FontFamily, cancellationToken);
    }

    public async Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        await _ui.InvokeAsync(() =>
        {
            SelectedSong = notification.Audio;
            _currentSongIndex = PlayList.IndexOf(SelectedSong);
        }, cancellationToken);
    }

    public async Task Handle(AdvancedSearchNotification notification, CancellationToken cancellationToken)
    {
        var result =
            await _advancedAudioReader.ReadAsync(notification.Filters, notification.MatchAll);
        await _ui.InvokeAsync(() =>
        {
            SwitchToSearchResultsTab();
            SearchResults.ReplaceWith(result);
        }, cancellationToken);
    }

    public async Task Handle(QuickSearchResultsNotification notification, CancellationToken cancellationToken)
    {
        await _ui.InvokeAsync(() =>
        {
            SwitchToSearchResultsTab();
            SearchResults.ReplaceWith(notification.Results);
        }, cancellationToken);
    }
    #endregion

    #region Commands
    [RelayCommand]
    public async Task JumpToSelectedSong()
    {
        if (SelectedIndex > -1)
        {
            await _mediaController.JumpToIndexAsync(SelectedIndex);
        }
    }

    [RelayCommand]
    private void SendSelectedToPlaylist()
    {
        if (!_selectedSearchResults.Any())
        {
            _logger.Verbose("Sending all {Count} search results to the playlist", SearchResults.Count);
            SendAllToPlaylist();
            return;
        }

        _logger.Verbose("Sending {Count} selected search results to the playlist", _selectedSearchResults.Count);
        _playlistStore.AddRange(_selectedSearchResults);

        while (_selectedSearchResults.Count > 0)
        {
            var toRemove = _selectedSearchResults.First();
            SearchResults.Remove(toRemove);
            _selectedSearchResults.Remove(toRemove);
        }
    }

    private void SendAllToPlaylist()
    {
        _playlistStore.AddRange(SearchResults);
        SearchResults.Clear();
        _selectedSearchResults.Clear();
    }

    [RelayCommand]
    private void RemoveSelectedFromPlaylist()
    {
        if (_selectedPlaylistItems.Count == 0)
        {
            _logger.Verbose("Removing all item from playlist");
            ClearPlaylist();
            return;
        }

        _logger.Verbose("Removing {Count} selected items from playlist", _selectedPlaylistItems.Count);
        while (_selectedPlaylistItems.Count > 0)
        {
            var toRemove = _selectedPlaylistItems.First();
            PlayList.Remove(toRemove);
            _selectedPlaylistItems.Remove(toRemove);
        }
    }

    private void ClearPlaylist()
    {
        _playlistStore.Clear();
        _selectedPlaylistItems.Clear();
    }

    [RelayCommand]
    private void SetSelectedSongAsNext()
    {
        if (SelectedSong?.Path is null) return;

        var newIndex = _currentSongIndex + 1;
        var snapshot = _playlistStore.Snapshot();
        if (snapshot.Count == 0) return;

        newIndex %= snapshot.Count;
        _playlistStore.MoveByPath(SelectedSong.Path, newIndex);
    }

    [RelayCommand]
    private async Task ScanSelectedSong(CancellationToken ct = default)
    {
        if (SelectedSong is null)
        {
            return;
        }

        _logger.Verbose("Scanning {SelectedSongTitle}", SelectedSong.Title);
        var scanned = await _fileScanner.ScanAsync(SelectedSong.Path!);
        var index = PlayList.IndexOf(SelectedSong);
        
        await _ui.InvokeAsync(() =>
        {
            PlayList[index] = scanned;
            SelectedSong = scanned;
        }, ct);
    }

    [RelayCommand]
    public void SwitchToSongMenuTab()
    {
        IsSearchResultsTabVisible = false;
        IsSongMenuTabVisible = true;
    }

    [RelayCommand]
    public void SwitchToSearchResultsTab()
    {
        IsSearchResultsTabVisible = true;
        IsSongMenuTabVisible = false;
    }
    #endregion

    public void AddSelectedSearchResult(AudioModel song)
    {
        _selectedSearchResults.Add(song);
    }

    public void RemoveSelectedSearchResult(AudioModel song)
    {
        _selectedSearchResults?.Remove(song);
    }

    public void AddSelectedPlaylistItems(AudioModel song)
    {
        _selectedPlaylistItems.Add(song);
    }

    public void RemoveSelectedPlaylistItems(AudioModel song)
    {
        _selectedPlaylistItems.Remove(song);
    }
    
    public void Dispose()
    {
        _playlistStore.Changed -= PlaylistStoreOnChanged;
    }
}