using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.Models;
using MediatR;

namespace Listen2MeRefined.Application.ViewModels.Controls;

public partial class PlaylistPaneViewModel :
    ViewModelBase,
    INotificationHandler<PlaylistViewModeChangedNotification>,
    INotificationHandler<PlaylistCreatedNotification>,
    INotificationHandler<PlaylistRenamedNotification>,
    INotificationHandler<PlaylistDeletedNotification>,
    INotificationHandler<PlaylistMembershipChangedNotification>,
    INotificationHandler<PlaylistShuffledNotification>,
    INotificationHandler<FontFamilyChangedNotification>
{
    private readonly ListsViewModel _lists;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IPlaylistLibraryService _playlistLibraryService;
    private readonly IMediator _mediator;

    private readonly HashSet<AudioModel> _selectedTabSongs = new();

    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private ObservableCollection<PlaylistTabItem> _tabs = new();
    [ObservableProperty] private PlaylistTabItem? _selectedTab;
    [ObservableProperty] private ObservableCollection<PlaylistSummary> _availablePlaylists = new();
    [ObservableProperty] private bool _isCompactPlaylistView;

    public ObservableCollection<AudioModel> PlayList => _lists.PlayList;

    public AudioModel? SelectedSong
    {
        get => _lists.SelectedSong;
        set => _lists.SelectedSong = value;
    }

    public int SelectedIndex
    {
        get => _lists.SelectedIndex;
        set => _lists.SelectedIndex = value;
    }

    public IRelayCommand RemoveSelectedFromPlaylistCommand => _lists.RemoveSelectedFromPlaylistCommand;
    public IAsyncRelayCommand JumpToSelectedSongCommand => _lists.JumpToSelectedSongCommand;
    public IRelayCommand SwitchToSongMenuTabCommand => _lists.SwitchToSongMenuTabCommand;

    public PlaylistPaneViewModel(
        ListsViewModel lists,
        IPlaylistLibraryService playlistLibraryService,
        IMediator mediator,
        IAppSettingsReader settingsReader)
    {
        _lists = lists;
        _settingsReader = settingsReader;
        _playlistLibraryService = playlistLibraryService;
        _mediator = mediator;
        _lists.PropertyChanged += ListsOnPropertyChanged;

        var defaultTab = new PlaylistTabItem("Default", null, _lists.DefaultPlaylist);
        Tabs = new ObservableCollection<PlaylistTabItem> { defaultTab };
        SelectedTab = defaultTab;
    }

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        await RefreshAvailablePlaylistsAsync(ct);
        IsCompactPlaylistView = _settingsReader.GetUseCompactPlaylistView();
    }

    [RelayCommand]
    private async Task OpenPlaylistTab(PlaylistSummary? playlist)
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

        var songs = await _playlistLibraryService.GetPlaylistSongsAsync(playlist.Id);
        var tab = new PlaylistTabItem(playlist.Name, playlist.Id, new ObservableCollection<AudioModel>(songs));
        Tabs.Add(tab);
        SelectedTab = tab;
    }

    [RelayCommand]
    private void CloseTab(PlaylistTabItem? tab)
    {
        if (tab is null || tab.IsDefaultTab)
        {
            return;
        }

        var wasSelected = ReferenceEquals(SelectedTab, tab);
        var wasActiveSource = _lists.ActiveNamedPlaylistId == tab.PlaylistId;

        Tabs.Remove(tab);

        if (wasSelected)
        {
            SelectedTab = Tabs.FirstOrDefault(x => x.IsDefaultTab) ?? Tabs.FirstOrDefault();
        }

        if (!wasActiveSource)
        {
            return;
        }

        var canContinue = _lists.SwitchActiveQueueToDefaultPreservingCurrentSong();
        if (!canContinue)
        {
            _lists.SwitchActiveQueueToDefaultAndStop();
        }
    }

    [RelayCommand]
    private async Task RemoveSelectedFromActiveTab()
    {
        var tab = SelectedTab;
        if (tab is null)
        {
            return;
        }

        var selectedSongs = GetSelectedSongsForContext();
        if (selectedSongs.Length == 0)
        {
            if (tab.IsDefaultTab)
            {
                _lists.RemoveFromDefaultPlaylist(_lists.DefaultPlaylist.ToArray());
                _selectedTabSongs.Clear();
                return;
            }

            if (tab.PlaylistId is null)
            {
                return;
            }

            var existingPaths = tab.Songs.Select(x => x.Path).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            tab.Songs.Clear();
            await _playlistLibraryService.RemoveSongsByPathAsync(tab.PlaylistId.Value, existingPaths);
            await _mediator.Publish(new PlaylistMembershipChangedNotification(tab.PlaylistId.Value));
            return;
        }

        if (tab.IsDefaultTab)
        {
            _lists.RemoveFromDefaultPlaylist(selectedSongs);
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

        if (_lists.ActiveNamedPlaylistId == tab.PlaylistId)
        {
            _lists.ActivateNamedPlaylistQueue(tab.PlaylistId.Value, tab.Songs);
        }

        _selectedTabSongs.Clear();
        await _mediator.Publish(new PlaylistMembershipChangedNotification(tab.PlaylistId.Value));
    }

    [RelayCommand]
    private async Task PlaySelectedFromActiveTab()
    {
        var tab = SelectedTab;
        if (tab is null || SelectedSong is null)
        {
            return;
        }

        if (tab.IsDefaultTab)
        {
            _lists.ActivateDefaultPlaylistQueue();
        }
        else if (tab.PlaylistId is not null)
        {
            _lists.ActivateNamedPlaylistQueue(tab.PlaylistId.Value, tab.Songs);
        }

        var jumpIndex = IndexOfSongByPath(_lists.PlayList, SelectedSong.Path);
        if (jumpIndex < 0)
        {
            return;
        }

        _lists.SelectedIndex = jumpIndex;
        _lists.SelectedSong = _lists.PlayList[jumpIndex];
        await _lists.JumpToSelectedSong();
    }

    [RelayCommand]
    private void PlaylistSelectionAdded(IList items)
    {
        foreach (var song in items.Cast<AudioModel>())
        {
            _selectedTabSongs.Add(song);
        }

        _lists.AddSelectedPlaylistItems(items.Cast<AudioModel>());
    }

    [RelayCommand]
    private void PlaylistSelectionRemoved(IList items)
    {
        foreach (var song in items.Cast<AudioModel>())
        {
            _selectedTabSongs.Remove(song);
        }

        _lists.RemoveSelectedPlaylistItems(items.Cast<AudioModel>());
    }

    public async Task<IReadOnlyList<PlaylistMenuState>> GetSongContextMenuPlaylistsAsync()
    {
        var selectedSongs = GetSelectedSongsForContext();
        if (selectedSongs.Length == 0)
        {
            return Array.Empty<PlaylistMenuState>();
        }

        if (selectedSongs.Length == 1 && !string.IsNullOrWhiteSpace(selectedSongs[0].Path))
        {
            var singleMembership = await _playlistLibraryService.GetMembershipBySongPathAsync(selectedSongs[0].Path!);
            return singleMembership
                .Select(x => new PlaylistMenuState(x.PlaylistId, x.PlaylistName, x.ContainsSong, AllowRemove: true))
                .ToArray();
        }

        var playlists = await _playlistLibraryService.GetAllPlaylistsAsync();
        return playlists
            .Select(x =>
            {
                var isCurrentNamed = SelectedTab is { IsDefaultTab: false } && SelectedTab.PlaylistId == x.Id;
                return new PlaylistMenuState(x.Id, x.Name, isCurrentNamed, AllowRemove: isCurrentNamed);
            })
            .ToArray();
    }

    public async Task TogglePlaylistMembershipAsync(int playlistId, bool shouldContain, bool allowRemove)
    {
        var selectedSongs = GetSelectedSongsForContext();
        if (selectedSongs.Length == 0)
        {
            return;
        }

        var paths = selectedSongs.Select(x => x.Path).ToArray();
        if (shouldContain)
        {
            await _playlistLibraryService.AddSongsByPathAsync(playlistId, paths);
        }
        else
        {
            if (!allowRemove)
            {
                return;
            }

            await _playlistLibraryService.RemoveSongsByPathAsync(playlistId, paths);
        }

        await _mediator.Publish(new PlaylistMembershipChangedNotification(playlistId));
    }

    public async Task AddToNewPlaylistFromContextAsync(string name)
    {
        var selectedSongs = GetSelectedSongsForContext();
        if (selectedSongs.Length == 0)
        {
            return;
        }

        var created = await _playlistLibraryService.CreatePlaylistAsync(name);
        await _playlistLibraryService.AddSongsByPathAsync(created.Id, selectedSongs.Select(x => x.Path));

        await RefreshAvailablePlaylistsAsync();
        await _mediator.Publish(new PlaylistCreatedNotification(created.Id, created.Name));
        await _mediator.Publish(new PlaylistMembershipChangedNotification(created.Id));
    }

    public async Task Handle(PlaylistCreatedNotification notification, CancellationToken cancellationToken)
    {
        await RefreshAvailablePlaylistsAsync(cancellationToken);

        var existing = Tabs.FirstOrDefault(x => x.PlaylistId == notification.PlaylistId);
        if (existing is not null)
        {
            existing.Header = notification.Name;
            SelectedTab = existing;
            return;
        }

        var songs = await _playlistLibraryService.GetPlaylistSongsAsync(notification.PlaylistId, cancellationToken);
        var tab = new PlaylistTabItem(notification.Name, notification.PlaylistId, new ObservableCollection<AudioModel>(songs));
        Tabs.Add(tab);
        SelectedTab = tab;
    }

    public async Task Handle(PlaylistRenamedNotification notification, CancellationToken cancellationToken)
    {
        await RefreshAvailablePlaylistsAsync(cancellationToken);
        var tab = Tabs.FirstOrDefault(x => x.PlaylistId == notification.PlaylistId);
        if (tab is not null)
        {
            tab.Header = notification.Name;
        }
    }

    public async Task Handle(PlaylistDeletedNotification notification, CancellationToken cancellationToken)
    {
        await RefreshAvailablePlaylistsAsync(cancellationToken);

        var tab = Tabs.FirstOrDefault(x => x.PlaylistId == notification.PlaylistId);
        if (tab is null)
        {
            return;
        }

        var wasActiveSource = _lists.ActiveNamedPlaylistId == notification.PlaylistId;
        Tabs.Remove(tab);
        SelectedTab ??= Tabs.FirstOrDefault(x => x.IsDefaultTab) ?? Tabs.FirstOrDefault();

        if (wasActiveSource)
        {
            _lists.SwitchActiveQueueToDefaultAndStop();
        }
    }

    public async Task Handle(PlaylistMembershipChangedNotification notification, CancellationToken cancellationToken)
    {
        var tab = Tabs.FirstOrDefault(x => x.PlaylistId == notification.PlaylistId);
        if (tab is null)
        {
            return;
        }

        var songs = await _playlistLibraryService.GetPlaylistSongsAsync(notification.PlaylistId, cancellationToken);
        tab.Songs.Clear();
        tab.Songs.AddRange(songs);

        if (_lists.ActiveNamedPlaylistId == notification.PlaylistId)
        {
            _lists.ActivateNamedPlaylistQueue(notification.PlaylistId, tab.Songs);
        }
    }

    public Task Handle(PlaylistShuffledNotification notification, CancellationToken cancellationToken)
    {
        var tab = SelectedTab;
        if (tab is null ||
            _lists.ActiveNamedPlaylistId != tab.PlaylistId ||
            tab.IsDefaultTab)   // Already handled by ListsViewModel.SyncDefaultPlaylistOrder
        {
            return Task.CompletedTask;
        }

        // Sync the named tab's Songs collection to match the shuffled _playList order
        var target = _lists.PlayList;
        var songs = tab.Songs;

        for (int i = 0; i < target.Count; i++)
        {
            var currentPos = songs.IndexOf(target[i]);
            if (currentPos >= 0 && currentPos != i)
                songs.Move(currentPos, i);
        }

        return Task.CompletedTask;
    }

    private async Task RefreshAvailablePlaylistsAsync(CancellationToken ct = default)
    {
        var allPlaylists = await _playlistLibraryService.GetAllPlaylistsAsync(ct);
        AvailablePlaylists = new ObservableCollection<PlaylistSummary>(allPlaylists);
    }

    private AudioModel[] GetSelectedSongsForContext()
    {
        var currentTabPaths = SelectedTab?.Songs
            .Where(x => !string.IsNullOrWhiteSpace(x.Path))
            .Select(x => x.Path!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (_selectedTabSongs.Count > 0)
        {
            var selectedSongs = _selectedTabSongs
                .Where(x => !string.IsNullOrWhiteSpace(x.Path))
                .Where(x => currentTabPaths is null || currentTabPaths.Contains(x.Path!))
                .Distinct()
                .ToArray();

            if (selectedSongs.Length > 0)
            {
                return selectedSongs;
            }
        }

        if (SelectedSong is not null
            && !string.IsNullOrWhiteSpace(SelectedSong.Path)
            && (currentTabPaths is null || currentTabPaths.Contains(SelectedSong.Path)))
        {
            return new[] { SelectedSong };
        }

        return Array.Empty<AudioModel>();
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
            if (!string.IsNullOrWhiteSpace(song.Path)
                && song.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    public Task HandleExternalFileDropAsync(IReadOnlyList<string> droppedPaths, int insertIndex, CancellationToken ct = default)
        => _lists.HandleExternalFileDropAsync(droppedPaths, insertIndex, ct);

    public Task Handle(PlaylistViewModeChangedNotification notification, CancellationToken cancellationToken)
    {
        IsCompactPlaylistView = notification.UseCompactPlaylistView;
        return Task.CompletedTask;
    }

    private void ListsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ListsViewModel.SelectedSong))
        {
            OnPropertyChanged(nameof(SelectedSong));
        }
        else if (e.PropertyName == nameof(ListsViewModel.SelectedIndex))
        {
            OnPropertyChanged(nameof(SelectedIndex));
        }
    }

    partial void OnSelectedTabChanged(PlaylistTabItem? value)
    {
        if (_selectedTabSongs.Count == 0)
        {
            return;
        }

        _lists.RemoveSelectedPlaylistItems(_selectedTabSongs);
        _selectedTabSongs.Clear();
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

    public sealed record PlaylistMenuState(int PlaylistId, string PlaylistName, bool IsChecked, bool AllowRemove);

    public Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        FontFamilyName = notification.FontFamily;
        return Task.CompletedTask;
    }
}
