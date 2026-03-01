using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Scanning.Files;
using Listen2MeRefined.Infrastructure.Searching;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.Infrastructure.Utils;

namespace Listen2MeRefined.Infrastructure.ViewModels.MainWindow;

public partial class ListsViewModel :
    ViewModelBase,
    INotificationHandler<CurrentSongNotification>,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<AdvancedSearchNotification>,
    INotificationHandler<QuickSearchResultsNotification>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IAudioSearchExecutionService _audioSearchExecutionService;
    private readonly IFileScanner _fileScanner;
    private readonly IMusicPlayerController _musicPlayerController;
    private readonly IPlaylist _playList;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAppSettingsWriter _settingsWriter;
    private readonly IDroppedSongFolderPromptService _droppedSongFolderPromptService;

    private static readonly HashSet<string> SupportedExtensions = new(
        GlobalConstants.SupportedExtensions,
        StringComparer.OrdinalIgnoreCase);

    private int _currentSongIndex = -1;
    private readonly HashSet<AudioModel> _selectedSearchResults = new();
    private readonly HashSet<AudioModel> _selectedPlaylistItems = new();

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private AudioModel? _selectedSong;
    [ObservableProperty] private int _selectedIndex = -1;
    [ObservableProperty] private ObservableCollection<AudioModel> _searchResults = new();
    [ObservableProperty] private bool _isSearchResultsTabVisible = true;
    [ObservableProperty] private bool _isSongMenuTabVisible;
    
    public ObservableCollection<AudioModel> PlayList => 
        _playList.Items as ObservableCollection<AudioModel> ??
        throw new InvalidOperationException("PlayList is not an ObservableCollection");

    public ListsViewModel(
        ILogger logger,
        IMediator mediator,
        IAudioSearchExecutionService audioSearchExecutionService,
        IFileScanner fileScanner,
        IMusicPlayerController musicPlayerController,
        IPlaylist playList,
        IAppSettingsReader settingsReader,
        IAppSettingsWriter settingsWriter,
        IDroppedSongFolderPromptService droppedSongFolderPromptService)
    {
        _logger = logger;
        _mediator = mediator;
        _audioSearchExecutionService = audioSearchExecutionService;
        _fileScanner = fileScanner;
        _musicPlayerController = musicPlayerController;
        _playList = playList;
        _settingsReader = settingsReader;
        _settingsWriter = settingsWriter;
        _droppedSongFolderPromptService = droppedSongFolderPromptService;

        _logger.Debug("[ListsViewModel] Class initialized");
    }

    public async Task HandleExternalFileDropAsync(IReadOnlyList<string> droppedPaths, int insertIndex, CancellationToken ct = default)
    {
        var supportedFiles = droppedPaths
            .Where(x => !string.IsNullOrWhiteSpace(x) && File.Exists(x))
            .Where(x => SupportedExtensions.Contains(Path.GetExtension(x)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (supportedFiles.Count == 0)
        {
            return;
        }

        var folders = supportedFiles
            .Select(Path.GetDirectoryName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        await PromptAndPersistMissingMusicFoldersAsync(folders, ct);

        var targetIndex = Math.Clamp(insertIndex, 0, PlayList.Count);
        foreach (var file in supportedFiles)
        {
            var scanned = await _fileScanner.ScanAsync(file, ct);
            PlayList.Insert(targetIndex, scanned);
            targetIndex++;
        }
    }

    private async Task PromptAndPersistMissingMusicFoldersAsync(IEnumerable<string> folders, CancellationToken ct)
    {
        var existing = _settingsReader.GetMusicFolders();
        var toAdd = existing.ToList();
        var mutedFolders = _settingsReader.GetMutedDroppedSongFolders().ToList();
        var changed = false;
        var mutedChanged = false;

        foreach (var folder in folders)
        {
            if (existing.Contains(folder, StringComparer.OrdinalIgnoreCase) ||
                toAdd.Contains(folder, StringComparer.OrdinalIgnoreCase) ||
                mutedFolders.Contains(folder, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var decision = await _droppedSongFolderPromptService.PromptAsync(folder, ct);
            if (decision == AddDroppedSongFolderDecision.AddFolder)
            {
                toAdd.Add(folder);
                changed = true;
            }
            else if (decision == AddDroppedSongFolderDecision.SkipAndDontAskAgain)
            {
                mutedFolders.Add(folder);
                mutedChanged = true;
            }
        }

        if (changed)
        {
            _settingsWriter.SetMusicFolders(toAdd);
        }

        if (mutedChanged)
        {
            _settingsWriter.SetMutedDroppedSongFolders(mutedFolders);
        }
    }

    [RelayCommand(CanExecute = nameof(CanJumpToSelectedSong))]
    public async Task JumpToSelectedSong()
    {
        _logger.Debug<int>("[ListsViewModel] Jumping to selected index {Index} in playlist", SelectedIndex);
        await _musicPlayerController.JumpToIndexAsync(SelectedIndex);
    }

    [RelayCommand]
    private void SendSelectedToPlaylist()
    {
        if (!_selectedSearchResults.Any())
        {
            _logger.Debug("[ListsViewModel] Sending all {Count} search results to the playlist", PlayList.Count);
            SendAllToPlaylist();
            return;
        }

        _logger.Debug("[ListsViewModel] Sending {Count} selected search results to the playlist", _selectedSearchResults.Count);
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
        if (_selectedPlaylistItems.Count == 0)
        {
            _logger.Debug($"[ListsViewModel] Removing all items from playlist");
            ClearPlaylist();
            return;
        }

        _logger.Debug($"[ListsViewModel] Removing selected items from playlist");
        foreach (var item in _selectedPlaylistItems)
        {
            PlayList.Remove(item);
        }

        _selectedPlaylistItems.Clear();
    }

    [RelayCommand]
    private void SetSelectedSongAsNext()
    {
        if (SelectedSong is null || PlayList.Count <= 1)
        {
            return;
        }

        _logger.Information<string?>("[ListsViewModel] Setting {Title} as next song", SelectedSong.Title);
        var selectedSongIndex = PlayList.IndexOf(SelectedSong);
        var newIndex = _currentSongIndex + 1;

        if (newIndex >= PlayList.Count)
        {
            newIndex = 0;
        }

        PlayList.Move(selectedSongIndex, newIndex);
        _logger.Debug("[ListsViewModel] Moved song from index {OldIndex} to {NewIndex}", selectedSongIndex, newIndex);
    }

    [RelayCommand]
    private async Task ScanSelectedSong()
    {
        if (SelectedSong is null)
        {
            _logger.Warning("[ListsViewModel] No song selected to scan");
            return;
        }

        _logger.Information<string?>("[ListsViewModel] Scanning {Title}", SelectedSong.Title);
        var scanned = await _fileScanner.ScanAsync(SelectedSong.Path!);
        var index = PlayList.IndexOf(SelectedSong);
        PlayList[index] = scanned;
        SelectedSong = scanned;
    }

    [RelayCommand]
    private void SwitchToSongMenuTab()
    {
        _logger.Verbose("[ListsViewModel] Switching to Song Menu tab");
        IsSearchResultsTabVisible = false;
        IsSongMenuTabVisible = true;
    }

    [RelayCommand]
    public void SwitchToSearchResultsTab()
    {
        _logger.Verbose("[ListsViewModel] Switching to Search Results tab");
        IsSearchResultsTabVisible = true;
        IsSongMenuTabVisible = false;
    }
    public void AddSelectedSearchResults(IEnumerable<AudioModel> songs)
    {
        foreach (var song in songs)
        {
            _selectedSearchResults.Add(song);
        }
    }

    public void RemoveSelectedSearchResults(IEnumerable<AudioModel> songs)
    {
        foreach (var song in songs)
        {
            _selectedSearchResults.Remove(song);
        }
    }

    public void AddSelectedPlaylistItems(IEnumerable<AudioModel> songs)
    {
        foreach (var song in songs)
        {
            _selectedPlaylistItems.Add(song);
        }
    }

    public void RemoveSelectedPlaylistItems(IEnumerable<AudioModel> songs)
    {
        foreach (var song in songs)
        {
            _selectedPlaylistItems.Remove(song);
        }
    }
    
    private bool CanJumpToSelectedSong() => SelectedIndex > -1;
    
    private void ClearPlaylist()
    {
        PlayList.Clear();
        _selectedPlaylistItems.Clear();
    }

    partial void OnSelectedIndexChanged(int value)
    {
        JumpToSelectedSongCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchResultsChanged(ObservableCollection<AudioModel> value)
    {
        _logger.Debug("[ListsViewModel] Search results changed with {Count} results", value.Count);
    }
    
    public async Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[ListsViewModel] Font family changed to {FontFamily}", notification.FontFamily);
        FontFamily = notification.FontFamily;
        await Task.CompletedTask;
    }

    public async Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[ListsViewModel] Current song changed to {@Audio}", notification.Audio);
        SelectedSong = notification.Audio;
        _currentSongIndex = PlayList.IndexOf(SelectedSong);
        await Task.CompletedTask;
    }

    public async Task Handle(AdvancedSearchNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[ListsViewModel] Performing advanced search with {@Filters} filters (MatchMode: {MatchMode})",
            notification.Filters, notification.MatchMode);
        var result =
            (await _audioSearchExecutionService.ExecuteAdvancedSearchAsync(notification.Filters, notification.MatchMode)).ToArray();

        _logger.Information("[ListsViewModel] Advanced search returned {Count} results", result.Length);
        if (result.Length > 0)
        {
            _logger.Verbose(
                "[ListsViewModel] First {Shown} results are: {@Results}",
                Math.Min(5, result.Length),
                result.Take(5));
        }

        SwitchToSearchResultsTab();
        SearchResults.Clear();
        SearchResults.AddRange(result);
        await _mediator.Publish(new AdvancedSearchCompletedNotification(result.Length), cancellationToken);
    }

    public async Task Handle(QuickSearchResultsNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Results.ToArray();

        _logger.Information("[ListsViewModel] Received quick search results with {Count} results", result.Length);
        if (result.Length > 0)
        {
            _logger.Verbose(
                "[ListsViewModel] First {Shown} results are: {@Results}",
                Math.Min(5, result.Length),
                result.Take(5));
        }

        SwitchToSearchResultsTab();
        SearchResults.Clear();
        Extensions.AddRange(SearchResults, notification.Results);
        await Task.CompletedTask;
    }
}
