namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Media;

public partial class ListsViewModel :
    ObservableObject,
    INotificationHandler<CurrentSongNotification>,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<AdvancedSearchNotification>,
    INotificationHandler<QuickSearchResultsNotification>
{
    private readonly ILogger _logger;
    private readonly IAdvancedDataReader<ParameterizedQuery, AudioModel> _advancedAudioReader;
    private readonly IFileScanner _fileScanner;
    private readonly IMediaController _mediaController;

    private int _currentSongIndex = -1;
    private readonly HashSet<AudioModel> _selectedSearchResults = new();
    private readonly HashSet<AudioModel> _selectedPlaylistItems = new();

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private AudioModel? _selectedSong;
    [ObservableProperty] private int _selectedIndex = -1;
    [ObservableProperty] private ObservableCollection<AudioModel> _searchResults = new();
    [ObservableProperty] private ObservableCollection<AudioModel> _playList = new();
    [ObservableProperty] private bool _isSearchResultsTabVisible = true;
    [ObservableProperty] private bool _isSongMenuTabVisible;

    public ListsViewModel(
        ILogger logger,
        IPlaylistReference playlistReference,
        IAdvancedDataReader<ParameterizedQuery, AudioModel> advancedAudioReader,
        IFileScanner fileScanner,
        IMediaController mediaController)
    {
        _logger = logger;

        playlistReference.PassPlaylist(ref _playList);
        _advancedAudioReader = advancedAudioReader;
        _fileScanner = fileScanner;
        _mediaController = mediaController;
    }

    #region Notifcation Handlers
    public async Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        FontFamily = notification.FontFamily;
        await Task.CompletedTask;
    }

    public async Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        SelectedSong = notification.Audio;
        _currentSongIndex = PlayList.IndexOf(SelectedSong);
        await Task.CompletedTask;
    }

    public async Task Handle(AdvancedSearchNotification notification, CancellationToken cancellationToken)
    {
        var result =
            await _advancedAudioReader.ReadAsync(notification.Filters, notification.MatchAll);
        SearchResults.Clear();
        SearchResults.AddRange(result);
    }

    public async Task Handle(QuickSearchResultsNotification notification, CancellationToken cancellationToken)
    {
        SwitchToSearchResultsTab();
        SearchResults.Clear();
        SearchResults.AddRange(notification.Results);
        await Task.CompletedTask;
    }
    #endregion

    #region Commands
    [RelayCommand]
    public async Task JumpToSelecteSong()
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
            _logger.Verbose("Sending all {Count} search results to the playlist", _selectedSearchResults.Count);
            SendAllToPlaylist();
            return;
        }

        _logger.Verbose("Sending {Count} selected search results to the playlist", _selectedSearchResults.Count);
        PlayList.AddRange(_selectedSearchResults);

        while (_selectedSearchResults.Count > 0)
        {
            var toRemove = _selectedSearchResults.First();
            SearchResults.Remove(toRemove);
            _selectedSearchResults.Remove(toRemove);
        }
    }

    private void SendAllToPlaylist()
    {
        PlayList.AddRange(SearchResults);
        SearchResults.Clear();
        _selectedSearchResults.Clear();
    }

    [RelayCommand]
    private void RemoveSelectedFromPlaylist()
    {
        if (_selectedSearchResults.Count == 0)
        {
            _logger.Verbose($"Removing all item from playlist");
            ClearPlaylist();
            return;
        }

        _logger.Verbose($"Removing selected items from playlist");
        while (_selectedPlaylistItems.Count > 0)
        {
            var toRemove = _selectedPlaylistItems.First();
            PlayList.Remove(toRemove);
            _selectedPlaylistItems.Remove(toRemove);
        }
    }

    private void ClearPlaylist()
    {
        PlayList.Clear();
        _selectedPlaylistItems.Clear();
    }

    [RelayCommand]
    private void SetSelectedSongAsNext()
    {
        if (SelectedSong is null || PlayList.Count <= 1)
        {
            return;
        }

        _logger.Verbose($"Setting {SelectedSong.Title} as next song");
        var selectedSongIndex = PlayList.IndexOf(SelectedSong);
        var newIndex = _currentSongIndex + 1;

        if (newIndex >= PlayList.Count)
        {
            newIndex = 0;
        }

        PlayList.Move(selectedSongIndex, newIndex);
    }

    [RelayCommand]
    private async Task ScanSelectedSong()
    {
        if (SelectedSong is null)
        {
            return;
        }

        _logger.Verbose($"Scanning {SelectedSong.Title}");
        var scanned = await _fileScanner.ScanAsync(SelectedSong.Path!);
        var index = PlayList.IndexOf(SelectedSong);
        PlayList[index] = scanned;
        SelectedSong = scanned;
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
}