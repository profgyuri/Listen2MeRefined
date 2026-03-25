using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.ContextMenus;
using Listen2MeRefined.Core.Models;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class PlaylistPaneViewModel : ViewModelBase
{
    private readonly IPlaylistQueueState _playlistQueueState;
    private readonly IPlaylistQueueRoutingService _playlistQueueRoutingService;
    private readonly IDefaultPlaylistService _defaultPlaylistService;
    private readonly IPlaybackQueueActionsService _playbackQueueActionsService;
    private readonly IExternalDropImportService _externalDropImportService;
    private readonly IPlaylistSelectionService _playlistSelectionService;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IPlaylistLibraryService _playlistLibraryService;
    private readonly IPlaybackContextSyncService _playbackContextSyncService;
    private readonly IExternalAudioOpenService _externalAudioOpenService;
    private readonly HashSet<AudioModel> _selectedTabSongs = [];

    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private ObservableCollection<PlaylistTabItem> _tabs = [];
    [ObservableProperty] private PlaylistTabItem? _selectedTab;
    [ObservableProperty] private ObservableCollection<PlaylistSummary> _availablePlaylists = [];
    [ObservableProperty] private bool _isCompactPlaylistView;

    public SongContextMenuViewModel SongContextMenuViewModel { get; }
    public ObservableCollection<AudioModel> PlayList => _playlistQueueState.PlayList;

    public AudioModel? SelectedSong
    {
        get => _playlistQueueState.SelectedSong;
        set => _playlistQueueState.SelectedSong = value;
    }

    public int SelectedIndex
    {
        get => _playlistQueueState.SelectedIndex;
        set => _playlistQueueState.SelectedIndex = value;
    }

    public PlaylistPaneViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IPlaylistQueueState playlistQueueState,
        IPlaylistQueueRoutingService playlistQueueRoutingService,
        IDefaultPlaylistService defaultPlaylistService,
        IPlaybackQueueActionsService playbackQueueActionsService,
        IExternalDropImportService externalDropImportService,
        IPlaylistSelectionService playlistSelectionService,
        IPlaylistLibraryService playlistLibraryService,
        IPlaybackContextSyncService playbackContextSyncService,
        IExternalAudioOpenService externalAudioOpenService,
        IAppSettingsReader settingsReader,
        SongContextMenuViewModel songContextMenuViewModel) : base(errorHandler, logger, messenger)
    {
        _playlistQueueState = playlistQueueState;
        _playlistQueueRoutingService = playlistQueueRoutingService;
        _defaultPlaylistService = defaultPlaylistService;
        _playbackQueueActionsService = playbackQueueActionsService;
        _externalDropImportService = externalDropImportService;
        _playlistSelectionService = playlistSelectionService;
        _settingsReader = settingsReader;
        _playlistLibraryService = playlistLibraryService;
        _playbackContextSyncService = playbackContextSyncService;
        _externalAudioOpenService = externalAudioOpenService;
        SongContextMenuViewModel = songContextMenuViewModel;
        _playlistQueueState.PropertyChanged += PlaylistQueueStateOnPropertyChanged;
    }

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
        RegisterMessage<PlaylistViewModeChangedMessage>(OnPlaylistViewModeChangedMessage);
        RegisterMessage<PlaylistCreatedMessage>(OnPlaylistCreatedMessage);
        RegisterMessage<PlaylistRenamedMessage>(OnPlaylistRenamedMessage);
        RegisterMessage<PlaylistDeletedMessage>(OnPlaylistDeletedMessage);
        RegisterMessage<PlaylistMembershipChangedMessage>(OnPlaylistMembershipChangedMessage);
        RegisterMessage<SearchResultsToPlaylistRequestedMessage>(OnSearchResultsToPlaylistRequestedMessage);
        RegisterMessage<CurrentSongChangedMessage>(OnCurrentSongChangedMessage);
        RegisterMessage<ExternalAudioFilesOpenedMessage>(OnExternalAudioFilesOpenedMessage);
        RegisterMessage<PlaylistShuffledMessage>(OnPlaylistShuffledMessage);

        var defaultTab = new PlaylistTabItem("Default", null, _playlistQueueState.DefaultPlaylist);
        Tabs = [defaultTab];
        SelectedTab = defaultTab;
        await RefreshAvailablePlaylistsAsync(ct);
        FontFamilyName = _settingsReader.GetFontFamily();
        IsCompactPlaylistView = _settingsReader.GetUseCompactPlaylistView();
        SongContextMenuViewModel.SetHost(this);
        await SongContextMenuViewModel.EnsureInitializedAsync(ct);
    }

    [RelayCommand]
    private Task OpenPlaylistTab(PlaylistSummary? playlist) =>
        ExecuteSafeAsync(async ct =>
        {
            if (playlist is null)
            {
                return;
            }

            var existing = Tabs.FirstOrDefault(x => x.PlaylistId == playlist.Id);
            if (existing is not null)
            {
                SelectedTab = existing;
                return;
            }

            var songs = await _playlistLibraryService.GetPlaylistSongsAsync(playlist.Id, ct);
            var tab = new PlaylistTabItem(playlist.Name, playlist.Id, new ObservableCollection<AudioModel>(songs));
            Tabs.Add(tab);
            SelectedTab = tab;
        });

    [RelayCommand]
    private Task CloseTab(PlaylistTabItem? tab) =>
        ExecuteSafeAsync(_ =>
        {
            if (tab is null || tab.IsDefaultTab)
            {
                return Task.CompletedTask;
            }

            var wasSelected = ReferenceEquals(SelectedTab, tab);
            var wasActiveSource = _playlistQueueState.ActiveNamedPlaylistId == tab.PlaylistId;
            Tabs.Remove(tab);

            if (wasSelected)
            {
                SelectedTab = Tabs.FirstOrDefault(x => x.IsDefaultTab) ??
                              Tabs.FirstOrDefault();
            }

            if (!wasActiveSource)
            {
                return Task.CompletedTask;
            }

            var canContinue = _playlistQueueRoutingService.SwitchActiveQueueToDefaultPreservingCurrentSong();
            if (!canContinue)
            {
                _playlistQueueRoutingService.SwitchActiveQueueToDefaultAndStop();
            }

            return Task.CompletedTask;
        });

    /// <summary>
    /// Removes selected songs from the active tab, or clears the tab when there is no explicit selection.
    /// </summary>
    [RelayCommand]
    private Task RemoveSelectedFromActiveTab() =>
        ExecuteSafeAsync(async _ =>
        {
            var tab = SelectedTab;
            if (tab is null)
            {
                return;
            }

            var selectedSongs = _playlistSelectionService.ResolveSelectedSongs(
                _selectedTabSongs,
                tab.Songs,
                SelectedSong);

            if (selectedSongs.Length == 0)
            {
                if (tab.IsDefaultTab)
                {
                    _defaultPlaylistService.RemoveFromDefaultPlaylist(_playlistQueueState.DefaultPlaylist.ToArray());
                    _selectedTabSongs.Clear();
                    return;
                }

                if (tab.PlaylistId is null)
                {
                    return;
                }

                var existingPaths = tab.Songs
                    .Where(x => !string.IsNullOrWhiteSpace(x.Path))
                    .Select(x => x.Path!)
                    .ToArray();
                tab.Songs.Clear();
                await _playlistLibraryService.RemoveSongsByPathAsync(tab.PlaylistId.Value, existingPaths);
                Messenger.Send(new PlaylistMembershipChangedMessage(tab.PlaylistId.Value));
                
                return;
            }

            if (tab.IsDefaultTab)
            {
                _defaultPlaylistService.RemoveFromDefaultPlaylist(selectedSongs);
                _selectedTabSongs.Clear();
                return;
            }

            if (tab.PlaylistId is null)
            {
                return;
            }

            await _playlistLibraryService.RemoveSongsByPathAsync(tab.PlaylistId.Value, selectedSongs.Select(x => x.Path));
            foreach (var song in selectedSongs)
            {
                tab.Songs.Remove(song);
            }

            if (_playlistQueueState.ActiveNamedPlaylistId == tab.PlaylistId)
            {
                _playlistQueueRoutingService.ActivateNamedPlaylistQueue(tab.PlaylistId.Value, tab.Songs);
            }

            _selectedTabSongs.Clear();
            Messenger.Send(new PlaylistMembershipChangedMessage(tab.PlaylistId.Value));
        });

    [RelayCommand(CanExecute = nameof(CanJumpToSelectedSong))]
    private Task JumpToSelectedSong() =>
        ExecuteSafeAsync(_ => _playbackQueueActionsService.JumpToSelectedSongAsync());

    [RelayCommand]
    private Task SetSelectedSongAsNext() =>
        ExecuteSafeAsync(_ =>
        {
            _playbackQueueActionsService.SetSelectedSongAsNext();
            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task ScanSelectedSong() =>
        ExecuteSafeAsync(_ => _playbackQueueActionsService.ScanSelectedSongAsync());

    /// <summary>
    /// Activates the selected tab as playback source and jumps to the currently selected song.
    /// </summary>
    [RelayCommand]
    private Task PlaySelectedFromActiveTab() =>
        ExecuteSafeAsync(async _ =>
        {
            var tab = SelectedTab;
            if (tab is null || SelectedSong is null)
            {
                return;
            }

            if (tab.IsDefaultTab)
            {
                _playlistQueueRoutingService.ActivateDefaultPlaylistQueue();
            }
            else if (tab.PlaylistId is not null)
            {
                _playlistQueueRoutingService.ActivateNamedPlaylistQueue(tab.PlaylistId.Value, tab.Songs);
            }

            var jumpIndex = IndexOfSongByPath(_playlistQueueState.PlayList, SelectedSong.Path);
            if (jumpIndex < 0)
            {
                return;
            }

            _playlistQueueState.SelectedIndex = jumpIndex;
            _playlistQueueState.SelectedSong = _playlistQueueState.PlayList[jumpIndex];
            await _playbackQueueActionsService.JumpToSelectedSongAsync();
        });

    [RelayCommand]
    private Task PlaylistSelectionAdded(IList items) =>
        ExecuteSafeAsync(_ =>
        {
            var songs = items.Cast<AudioModel>().ToArray();
            foreach (var song in songs)
            {
                _selectedTabSongs.Add(song);
            }

            PublishSongContextSelectionChanged();
            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task PlaylistSelectionRemoved(IList items) =>
        ExecuteSafeAsync(_ =>
        {
            var songs = items.Cast<AudioModel>().ToArray();
            foreach (var song in songs)
            {
                _selectedTabSongs.Remove(song);
            }

            PublishSongContextSelectionChanged();
            return Task.CompletedTask;
        });

    private async Task RefreshAvailablePlaylistsAsync(CancellationToken ct = default)
    {
        var allPlaylists = await _playlistLibraryService.GetAllPlaylistsAsync(ct);
        AvailablePlaylists = new ObservableCollection<PlaylistSummary>(allPlaylists);
    }

    public IReadOnlyCollection<AudioModel> GetSelectedTabSongContextSelection() => _selectedTabSongs.ToArray();

    public IReadOnlyCollection<AudioModel> GetCurrentTabSongContextSelection() =>
        SelectedTab?.Songs?.ToArray() ?? [];

    public int? GetSongContextActivePlaylistId() =>
        SelectedTab is { IsDefaultTab: false } ? SelectedTab.PlaylistId : null;

    public Task HandleExternalFileDropAsync(IReadOnlyList<string> droppedPaths, int insertIndex, CancellationToken ct = default) =>
        _externalDropImportService.HandleExternalFileDropAsync(droppedPaths, insertIndex, ct);

    private void PlaylistQueueStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IPlaylistQueueState.SelectedSong))
        {
            OnPropertyChanged(nameof(SelectedSong));
            PublishSongContextSelectionChanged();
            JumpToSelectedSongCommand.NotifyCanExecuteChanged();
        }
        else if (e.PropertyName == nameof(IPlaylistQueueState.SelectedIndex))
        {
            OnPropertyChanged(nameof(SelectedIndex));
            JumpToSelectedSongCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnSelectedTabChanged(PlaylistTabItem? value)
    {
        if (_selectedTabSongs.Count == 0)
        {
            return;
        }

        _selectedTabSongs.Clear();
        PublishSongContextSelectionChanged();
    }
    
    private static int IndexOfSongByPath(IEnumerable<AudioModel> songs, string? path)
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

    private void PublishSongContextSelectionChanged()
    {
        Messenger.Send(new SongContextMenuSelectionChangedMessage(this));
    }

    private bool CanJumpToSelectedSong()
    {
        return _playbackQueueActionsService.CanJumpToSelectedSong();
    }

    private async void OnPlaylistCreatedMessage(PlaylistCreatedMessage message)
    {
        try
        {
            var payload = message.Value;
            await RefreshAvailablePlaylistsAsync();

            var existing = Tabs.FirstOrDefault(x => x.PlaylistId == payload.PlaylistId);
            if (existing is not null)
            {
                existing.Header = payload.Name;
                SelectedTab = existing;
                return;
            }

            var songs = await _playlistLibraryService.GetPlaylistSongsAsync(payload.PlaylistId);
            var tab = new PlaylistTabItem(payload.Name, payload.PlaylistId,
                new ObservableCollection<AudioModel>(songs));
            Tabs.Add(tab);
            SelectedTab = tab;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to create playlist");
        }
    }

    private async void OnPlaylistRenamedMessage(PlaylistRenamedMessage message)
    {
        try
        {
            await RefreshAvailablePlaylistsAsync();
            var tab = Tabs.FirstOrDefault(x => x.PlaylistId == message.Value.PlaylistId);
            tab?.Header = message.Value.Name;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to rename playlist");
        }
    }

    private async void OnPlaylistDeletedMessage(PlaylistDeletedMessage message)
    {
        try
        {
            var playlistId = message.Value.PlaylistId;
            await RefreshAvailablePlaylistsAsync();

            var tab = Tabs.FirstOrDefault(x => x.PlaylistId == playlistId);
            if (tab is null)
            {
                return;
            }

            var wasActiveSource = _playlistQueueState.ActiveNamedPlaylistId == playlistId;
            Tabs.Remove(tab);
            SelectedTab ??= Tabs.FirstOrDefault(x => x.IsDefaultTab) ??
                            Tabs.FirstOrDefault();

            if (wasActiveSource)
            {
                _playlistQueueRoutingService.SwitchActiveQueueToDefaultAndStop();
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to delete playlist");
        }
    }

    private async void OnPlaylistMembershipChangedMessage(PlaylistMembershipChangedMessage message)
    {
        try
        {
            var playlistId = message.Value;
            var tab = Tabs.FirstOrDefault(x => x.PlaylistId == playlistId);
            if (tab is null)
            {
                return;
            }

            var songs = await _playlistLibraryService.GetPlaylistSongsAsync(playlistId);
            tab.Songs.Clear();
            tab.Songs.AddRange(songs);

            if (_playlistQueueState.ActiveNamedPlaylistId == playlistId)
            {
                _playlistQueueRoutingService.ActivateNamedPlaylistQueue(playlistId, tab.Songs);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to update playlist membership");
        }
    }

    private void OnPlaylistShuffledMessage(PlaylistShuffledMessage message)
    {
        try
        {
            _playlistQueueRoutingService.SyncDefaultPlaylistOrder();

            var tab = SelectedTab;
            if (tab is null ||
                _playlistQueueState.ActiveNamedPlaylistId != tab.PlaylistId ||
                tab.IsDefaultTab)
            {
                return;
            }

            var target = _playlistQueueState.PlayList;
            var songs = tab.Songs;

            for (int i = 0; i < target.Count; i++)
            {
                var currentPos = songs.IndexOf(target[i]);
                if (currentPos >= 0 && currentPos != i)
                {
                    songs.Move(currentPos, i);
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to shuffle playlist");
        }
    }

    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        FontFamilyName = message.Value;
    }

    private void OnPlaylistViewModeChangedMessage(PlaylistViewModeChangedMessage message)
    {
        IsCompactPlaylistView = message.Value;
    }

    private void OnSearchResultsToPlaylistRequestedMessage(SearchResultsToPlaylistRequestedMessage message)
    {
        _defaultPlaylistService.AddSearchResultsToDefaultPlaylist(message.Value);
    }

    private void OnCurrentSongChangedMessage(CurrentSongChangedMessage message)
    {
        Logger.Information("[PlaylistPaneViewModel] Current song changed to {@Audio}", message.Value);
        _externalAudioOpenService.SetCurrentSong(message.Value);
        _playbackContextSyncService.SetCurrentSong(message.Value);
    }

    private void OnExternalAudioFilesOpenedMessage(ExternalAudioFilesOpenedMessage message)
    {
        Logger.Information(
            "[PlaylistPaneViewModel] Handling {Count} shell-opened audio file(s)",
            message.Value.Count);
        _ = ExecuteSafeAsync(ct => _externalAudioOpenService.OpenAsync(message.Value, ct));
    }
    
    public sealed class PlaylistTabItem : ObservableObject
    {
        private string _header;

        public PlaylistTabItem(string header, int? playlistId, ObservableCollection<AudioModel> songs)
        {
            _header = header;
            PlaylistId = playlistId;
            Songs = songs;
        }

        public int? PlaylistId { get; }
        public bool IsDefaultTab => PlaylistId is null;
        public bool IsCloseable => !IsDefaultTab;
        public ObservableCollection<AudioModel> Songs { get; }

        public string Header
        {
            get => _header;
            set
            {
                if (_header == value)
                {
                    return;
                }

                _header = value;
                OnPropertyChanged();
            }
        }
    }
}
