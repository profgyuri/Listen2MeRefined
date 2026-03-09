using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using MediatR;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Controls;

public partial class ListsViewModel :
    ViewModelBase,
    INotificationHandler<CurrentSongNotification>,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<AdvancedSearchNotification>,
    INotificationHandler<QuickSearchResultsNotification>,
    INotificationHandler<ExternalAudioFilesOpenedNotification>,
    INotificationHandler<PlaylistShuffledNotification>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IAudioSearchExecutionService _audioSearchExecutionService;
    private readonly IFileScanner _fileScanner;
    private readonly IMusicPlayerController _musicPlayerController;
    private readonly IPlaylist _playList;
    private readonly IExternalAudioOpenService _externalAudioOpenService;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAppSettingsWriter _settingsWriter;
    private readonly IDroppedSongFolderPromptService _droppedSongFolderPromptService;
    private readonly IUiDispatcher _ui;

    private static readonly HashSet<string> SupportedExtensions = new(
        GlobalConstants.SupportedExtensions,
        StringComparer.OrdinalIgnoreCase);

    private int _currentSongIndex = -1;
    private int? _activeNamedPlaylistId;
    private readonly HashSet<AudioModel> _selectedSearchResults = new();
    private readonly HashSet<AudioModel> _selectedPlaylistItems = new();
    private readonly ObservableCollection<AudioModel> _defaultPlaylist = new();

    [ObservableProperty] private string? _fontFamilyName = string.Empty;
    [ObservableProperty] private AudioModel? _selectedSong;
    [ObservableProperty] private int _selectedIndex = -1;
    [ObservableProperty] private ObservableCollection<AudioModel> _searchResults = new();
    [ObservableProperty] private bool _isSearchResultsTabVisible = true;
    [ObservableProperty] private bool _isSongMenuTabVisible;
    
    public ObservableCollection<AudioModel> PlayList => 
        _playList.Items as ObservableCollection<AudioModel> ??
        throw new InvalidOperationException("PlayList is not an ObservableCollection");
    public ObservableCollection<AudioModel> DefaultPlaylist => _defaultPlaylist;
    public int? ActiveNamedPlaylistId => _activeNamedPlaylistId;
    public bool IsDefaultPlaylistActive => _activeNamedPlaylistId is null;

    public ListsViewModel(
        ILogger logger,
        IMediator mediator,
        IAudioSearchExecutionService audioSearchExecutionService,
        IFileScanner fileScanner,
        IAppSettingsReader settingsReader,
        IMusicPlayerController musicPlayerController,
        IPlaylist playList,
        IAppSettingsWriter settingsWriter,
        IDroppedSongFolderPromptService droppedSongFolderPromptService,
        IExternalAudioOpenService externalAudioOpenService, IUiDispatcher ui)
    {
        _logger = logger;
        _mediator = mediator;
        _audioSearchExecutionService = audioSearchExecutionService;
        _fileScanner = fileScanner;
        _musicPlayerController = musicPlayerController;
        _playList = playList;
        _externalAudioOpenService = externalAudioOpenService;
        _ui = ui;
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

        var scannedSongs = new List<AudioModel>(supportedFiles.Count);
        foreach (var file in supportedFiles)
        {
            var scanned = await _fileScanner.ScanAsync(file, ct);
            scannedSongs.Add(scanned);
        }

        var defaultTargetIndex = Math.Clamp(insertIndex, 0, _defaultPlaylist.Count);
        foreach (var song in scannedSongs)
        {
            _defaultPlaylist.Insert(defaultTargetIndex, song);
            defaultTargetIndex++;
        }

        if (!IsDefaultPlaylistActive)
        {
            return;
        }

        var playListTargetIndex = Math.Clamp(insertIndex, 0, PlayList.Count);
        foreach (var song in scannedSongs)
        {
            PlayList.Insert(playListTargetIndex, song);
            playListTargetIndex++;
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
        if (!CanJumpToSelectedSong())
        {
            return;
        }

        _logger.Debug("[ListsViewModel] Jumping to selected index {Index} in playlist", SelectedIndex);
        await _musicPlayerController.JumpToIndexAsync(SelectedIndex);
    }

    [RelayCommand]
    private void SendSelectedToPlaylist()
    {
        var transferMode = _settingsReader.GetSearchResultsTransferMode();
        if (!_selectedSearchResults.Any())
        {
            _logger.Debug("[ListsViewModel] Sending all {Count} search results to the default tab", SearchResults.Count);
            SendAllToDefaultPlaylist(transferMode);
            return;
        }

        _logger.Debug("[ListsViewModel] Sending {Count} selected search results to the default tab", _selectedSearchResults.Count);
        AddUniqueToDefaultPlaylist(_selectedSearchResults);

        if (transferMode == SearchResultsTransferMode.Move)
        {
            while (_selectedSearchResults.Count > 0)
            {
                var toRemove = _selectedSearchResults.First();
                SearchResults.Remove(toRemove);
                _selectedSearchResults.Remove(toRemove);
            }
            return;
        }

        _selectedSearchResults.Clear();
    }

    private void SendAllToDefaultPlaylist(SearchResultsTransferMode transferMode)
    {
        AddUniqueToDefaultPlaylist(SearchResults);
        if (transferMode == SearchResultsTransferMode.Move)
        {
            SearchResults.Clear();
        }
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
            if (IsDefaultPlaylistActive)
            {
                _defaultPlaylist.Remove(item);
            }
        }

        _selectedPlaylistItems.Clear();
    }

    [RelayCommand]
    private void SetSelectedSongAsNext()
    {
        if (SelectedSong is null || PlayList.Count <= 1 || !IsSongInActiveQueue(SelectedSong))
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
        if (index >= 0)
        {
            PlayList[index] = scanned;
        }

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

    public IReadOnlyCollection<AudioModel> GetSelectedSearchResults()
    {
        return _selectedSearchResults.ToArray();
    }

    public bool IsSongInActiveQueue(AudioModel? song)
    {
        if (song is null || string.IsNullOrWhiteSpace(song.Path))
        {
            return false;
        }

        return PlayList.Any(x =>
            !string.IsNullOrWhiteSpace(x.Path) &&
            x.Path.Equals(song.Path, StringComparison.OrdinalIgnoreCase));
    }

    public void ActivateDefaultPlaylistQueue()
    {
        ReplacePlaybackQueue(_defaultPlaylist);
        _activeNamedPlaylistId = null;
    }

    public void ActivateNamedPlaylistQueue(int playlistId, IEnumerable<AudioModel> songs)
    {
        ReplacePlaybackQueue(songs);
        _activeNamedPlaylistId = playlistId;
    }

    public bool SwitchActiveQueueToDefaultPreservingCurrentSong()
    {
        var currentSongPath = SelectedSong?.Path;
        ReplacePlaybackQueue(_defaultPlaylist);
        _activeNamedPlaylistId = null;

        var index = IndexOfPath(PlayList, currentSongPath);
        if (index < 0)
        {
            return false;
        }

        _playList.CurrentIndex = index;
        SelectedIndex = index;
        return true;
    }

    public void SwitchActiveQueueToDefaultAndStop()
    {
        ReplacePlaybackQueue(_defaultPlaylist);
        _activeNamedPlaylistId = null;
        _musicPlayerController.Stop();
    }

    public void RemoveFromDefaultPlaylist(IEnumerable<AudioModel> songs)
    {
        var candidates = songs
            .Where(x => !string.IsNullOrWhiteSpace(x.Path))
            .Select(x => x.Path!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (candidates.Count == 0)
        {
            return;
        }

        var toRemove = _defaultPlaylist
            .Where(x => !string.IsNullOrWhiteSpace(x.Path) && candidates.Contains(x.Path!))
            .ToArray();

        foreach (var song in toRemove)
        {
            _defaultPlaylist.Remove(song);
            if (IsDefaultPlaylistActive)
            {
                PlayList.Remove(song);
            }
        }
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
    
    private bool CanJumpToSelectedSong()
    {
        return SelectedIndex > -1 && IsSongInActiveQueue(SelectedSong);
    }
    
    private void ClearPlaylist()
    {
        if (IsDefaultPlaylistActive)
        {
            _defaultPlaylist.Clear();
        }

        PlayList.Clear();
        _selectedPlaylistItems.Clear();
    }

    partial void OnSelectedIndexChanged(int value)
    {
        _ui.InvokeAsync(() => JumpToSelectedSongCommand.NotifyCanExecuteChanged());
    }

    partial void OnSelectedSongChanged(AudioModel? value)
    {
        _ui.InvokeAsync(() => JumpToSelectedSongCommand.NotifyCanExecuteChanged());
    }

    partial void OnSearchResultsChanged(ObservableCollection<AudioModel> value)
    {
        _logger.Debug("[ListsViewModel] Search results changed with {Count} results", value.Count);
    }
    
    public async Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[ListsViewModel] Font family changed to {FontFamily}", notification.FontFamily);
        FontFamilyName = notification.FontFamily;
        await Task.CompletedTask;
    }

    public async Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[ListsViewModel] Current song changed to {@Audio}", notification.Audio);
        _externalAudioOpenService.SetCurrentSong(notification.Audio);
        SelectedSong = notification.Audio;
        _currentSongIndex = PlayList.IndexOf(SelectedSong);
        await Task.CompletedTask;
    }

    public Task Handle(ExternalAudioFilesOpenedNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[ListsViewModel] Handling {Count} shell-opened audio file(s)", notification.Paths.Count);
        return _externalAudioOpenService.OpenAsync(notification.Paths, cancellationToken);
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

    public async Task Handle(PlaylistShuffledNotification notification, CancellationToken cancellationToken)
    {
        await _ui.InvokeAsync(SyncDefaultPlaylistOrder, cancellationToken);
    }
    
    private void SyncDefaultPlaylistOrder()
    {
        var target = _playList.Items;
        for (int i = 0; i < target.Count; i++)
        {
            var currentPos = _defaultPlaylist.IndexOf(target[i]);
            if (currentPos != i)
                _defaultPlaylist.Move(currentPos, i);
        }
    }

    private void AddUniqueToDefaultPlaylist(IEnumerable<AudioModel> songs)
    {
        var existingPaths = _defaultPlaylist
            .Where(x => !string.IsNullOrWhiteSpace(x.Path))
            .Select(x => x.Path!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var song in songs)
        {
            if (string.IsNullOrWhiteSpace(song.Path) || existingPaths.Contains(song.Path))
            {
                continue;
            }

            _defaultPlaylist.Add(song);
            existingPaths.Add(song.Path);
            if (IsDefaultPlaylistActive && !PlayList.Any(x =>
                    !string.IsNullOrWhiteSpace(x.Path) &&
                    x.Path!.Equals(song.Path, StringComparison.OrdinalIgnoreCase)))
            {
                PlayList.Add(song);
            }
        }
    }

    private void ReplacePlaybackQueue(IEnumerable<AudioModel> songs)
    {
        var uniquePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uniqueSongs = songs
            .Where(x => !string.IsNullOrWhiteSpace(x.Path) && uniquePaths.Add(x.Path!))
            .ToArray();

        PlayList.Clear();
        PlayList.AddRange(uniqueSongs);

        if (PlayList.Count == 0)
        {
            _playList.CurrentIndex = 0;
            _currentSongIndex = -1;
            return;
        }

        var currentPath = SelectedSong?.Path;
        var matchingIndex = IndexOfPath(PlayList, currentPath);
        if (matchingIndex >= 0)
        {
            _playList.CurrentIndex = matchingIndex;
            _currentSongIndex = matchingIndex;
            return;
        }

        _playList.CurrentIndex = 0;
        _currentSongIndex = 0;
    }

    private static int IndexOfPath(IEnumerable<AudioModel> songs, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return -1;
        }

        var index = 0;
        foreach (var song in songs)
        {
            if (!string.IsNullOrWhiteSpace(song.Path) &&
                song.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }

            index++;
        }

        return -1;
    }
}
