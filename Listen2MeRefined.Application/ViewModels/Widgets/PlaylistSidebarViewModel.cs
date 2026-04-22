using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Playlist.Formats;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public sealed partial class PlaylistSidebarViewModel : ViewModelBase
{
    private readonly IPlaylistLibraryService _playlistLibraryService;
    private readonly IPlaylistQueueState _playlistQueueState;
    private readonly IPlaylistFormatRegistry _formatRegistry;
    private readonly IPlaylistImportService _importService;
    private readonly IPlaylistExportService _exportService;
    private readonly IFileDialogService _fileDialogService;

    [ObservableProperty] private PlaylistSidebarItem? _selectedItem;

    public PlaylistSidebarItem DefaultPlaylist { get; } = new(null, "Default");

    public ObservableCollection<PlaylistSidebarItem> ManualPlaylists { get; } = [];

    /// <summary>
    /// Format choices surfaced in the export submenu.
    /// </summary>
    public IReadOnlyList<PlaylistExportFormatOption> ExportFormatOptions { get; }

    /// <summary>
    /// Tooltip text for the Import Playlist button — enumerates accepted formats.
    /// </summary>
    public string ImportTooltip { get; }

    public PlaylistSidebarViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IPlaylistLibraryService playlistLibraryService,
        IPlaylistQueueState playlistQueueState,
        IPlaylistFormatRegistry formatRegistry,
        IPlaylistImportService importService,
        IPlaylistExportService exportService,
        IFileDialogService fileDialogService) : base(errorHandler, logger, messenger)
    {
        _playlistLibraryService = playlistLibraryService;
        _playlistQueueState = playlistQueueState;
        _formatRegistry = formatRegistry;
        _importService = importService;
        _exportService = exportService;
        _fileDialogService = fileDialogService;

        ExportFormatOptions = _formatRegistry.Formats
            .Select(f => new PlaylistExportFormatOption(f))
            .ToList();

        ImportTooltip = BuildImportTooltip(_formatRegistry.Formats);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var playlists = await _playlistLibraryService.GetAllPlaylistsAsync(cancellationToken);
        foreach (var p in playlists)
        {
            ManualPlaylists.Add(new PlaylistSidebarItem(p.Id, p.Name) { IsPinned = p.IsPinned });
        }

        _playlistQueueState.PropertyChanged += OnQueueStatePropertyChanged;
        RefreshActiveIndicators();

        RegisterMessage<PlaylistCreatedMessage>(OnPlaylistCreated);
        RegisterMessage<PlaylistDeletedMessage>(OnPlaylistDeleted);
        RegisterMessage<PlaylistRenamedMessage>(OnPlaylistRenamed);
        RegisterMessage<SelectPlaylistRequestedMessage>(OnSelectPlaylistRequested);

        // Select default playlist on startup
        SelectPlaylist(DefaultPlaylist);
    }

    [RelayCommand]
    private void SelectPlaylist(PlaylistSidebarItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (SelectedItem is not null)
        {
            SelectedItem.IsSelected = false;
        }

        SelectedItem = item;
        item.IsSelected = true;
        Messenger.Send(new PlaylistSidebarSelectionChangedMessage(new PlaylistSidebarSelectionData(item.PlaylistId)));
    }

    [RelayCommand]
    private void BeginRename(PlaylistSidebarItem? item)
    {
        if (item is null || item.IsDefault)
        {
            return;
        }

        item.OriginalName = item.Name;
        item.IsRenaming = true;
    }

    [RelayCommand]
    private async Task CommitRename(PlaylistSidebarItem? item)
    {
        if (item is null || !item.IsRenaming)
        {
            return;
        }

        var newName = item.Name?.Trim() ?? string.Empty;
        if (newName.Length < 2 || newName.Length > 50)
        {
            CancelRename(item);
            return;
        }

        await ExecuteSafeAsync(async ct =>
        {
            await _playlistLibraryService.RenamePlaylistAsync(item.PlaylistId!.Value, newName, ct);
            item.Name = newName;
            item.IsRenaming = false;
            Messenger.Send(new PlaylistRenamedMessage(new PlaylistRenamedMessageData(item.PlaylistId!.Value, newName)));
        });
    }

    [RelayCommand]
    private void CancelRename(PlaylistSidebarItem? item)
    {
        if (item is null || !item.IsRenaming)
        {
            return;
        }

        item.Name = item.OriginalName ?? item.Name;
        item.IsRenaming = false;
    }

    [RelayCommand]
    private async Task TogglePin(PlaylistSidebarItem? item)
    {
        if (item is null || item.IsDefault)
        {
            return;
        }

        var newPinned = !item.IsPinned;
        await ExecuteSafeAsync(async ct =>
        {
            await _playlistLibraryService.SetPinnedAsync(item.PlaylistId!.Value, newPinned, ct);
            item.IsPinned = newPinned;
            ResortManualPlaylists();
        });
    }

    [RelayCommand]
    private async Task DeletePlaylist(PlaylistSidebarItem? item)
    {
        if (item is null || item.IsDefault)
        {
            return;
        }

        await ExecuteSafeAsync(async ct =>
        {
            await _playlistLibraryService.DeletePlaylistAsync(item.PlaylistId!.Value, ct);
            ManualPlaylists.Remove(item);

            if (SelectedItem == item)
            {
                SelectPlaylist(DefaultPlaylist);
            }

            Messenger.Send(new PlaylistDeletedMessage(new PlaylistDeletedMessageData(item.PlaylistId!.Value)));
        });
    }

    [RelayCommand]
    private async Task CreatePlaylist()
    {
        await ExecuteSafeAsync(async ct =>
        {
            var summary = await _playlistLibraryService.CreatePlaylistAsync("New Playlist", ct);
            var newItem = new PlaylistSidebarItem(summary.Id, summary.Name) { IsPinned = summary.IsPinned };
            ManualPlaylists.Add(newItem);
            SelectPlaylist(newItem);

            Messenger.Send(new PlaylistCreatedMessage(new PlaylistCreatedMessageData(summary.Id, summary.Name)));

            BeginRename(newItem);
        });
    }

    [RelayCommand]
    private async Task ReorderPlaylists()
    {
        var ordering = new List<(int PlaylistId, int NewOrder)>();
        for (var i = 0; i < ManualPlaylists.Count; i++)
        {
            var item = ManualPlaylists[i];
            if (item.PlaylistId.HasValue)
            {
                ordering.Add((item.PlaylistId.Value, i));
            }
        }

        await ExecuteSafeAsync(async ct =>
        {
            await _playlistLibraryService.ReorderPlaylistsAsync(ordering, ct);
        });
    }

    [RelayCommand]
    private async Task ImportPlaylist()
    {
        var filter = _formatRegistry.BuildOpenFilter();
        var path = _fileDialogService.PickOpenFile("Import playlist", filter);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await ExecuteSafeAsync(ct => _importService.ImportAsync(path, ct));
    }

    [RelayCommand]
    private async Task ExportPlaylist(PlaylistExportRequest? request)
    {
        if (request is null || request.Item is null || request.Format is null)
        {
            return;
        }

        var item = request.Item;
        var format = request.Format;

        var preferredExtension = format.Extensions.FirstOrDefault() ?? ".m3u8";
        var filter = $"{format.DisplayName}|*{preferredExtension}";
        var defaultFileName = BuildSafeFileName(item.Name) + preferredExtension;

        var target = _fileDialogService.PickSaveFile(
            $"Export {item.Name}",
            filter,
            defaultFileName,
            preferredExtension);

        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        var source = new PlaylistExportSource(item.PlaylistId, item.Name);
        await ExecuteSafeAsync(ct => _exportService.ExportAsync(source, target, format, ct));
    }

    private void OnPlaylistCreated(PlaylistCreatedMessage message)
    {
        var data = message.Value;
        if (ManualPlaylists.Any(x => x.PlaylistId == data.PlaylistId))
        {
            return;
        }

        ManualPlaylists.Add(new PlaylistSidebarItem(data.PlaylistId, data.Name));
    }

    private void OnPlaylistDeleted(PlaylistDeletedMessage message)
    {
        var item = ManualPlaylists.FirstOrDefault(x => x.PlaylistId == message.Value.PlaylistId);
        if (item is null)
        {
            return;
        }

        ManualPlaylists.Remove(item);
        if (SelectedItem == item)
        {
            SelectPlaylist(DefaultPlaylist);
        }
    }

    private void OnPlaylistRenamed(PlaylistRenamedMessage message)
    {
        var data = message.Value;
        var item = ManualPlaylists.FirstOrDefault(x => x.PlaylistId == data.PlaylistId);
        if (item is not null)
        {
            item.Name = data.Name;
        }
    }

    private void OnSelectPlaylistRequested(SelectPlaylistRequestedMessage message)
    {
        var target = ResolveSidebarItemById(message.Value);
        if (target is null)
        {
            return;
        }

        SelectPlaylist(target);
    }

    private PlaylistSidebarItem? ResolveSidebarItemById(int? playlistId)
    {
        if (playlistId is null)
        {
            return DefaultPlaylist;
        }

        return ManualPlaylists.FirstOrDefault(x => x.PlaylistId == playlistId);
    }

    private void OnQueueStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPlaylistQueueState.ActiveNamedPlaylistId)
            or nameof(IPlaylistQueueState.IsDefaultPlaylistActive))
        {
            RefreshActiveIndicators();
        }
    }

    private void RefreshActiveIndicators()
    {
        var activeId = _playlistQueueState.ActiveNamedPlaylistId;
        DefaultPlaylist.IsActive = _playlistQueueState.IsDefaultPlaylistActive;

        foreach (var item in ManualPlaylists)
        {
            item.IsActive = item.PlaylistId == activeId;
        }
    }

    private void ResortManualPlaylists()
    {
        var sorted = ManualPlaylists
            .OrderByDescending(x => x.IsPinned)
            .ThenBy(ManualPlaylists.IndexOf)
            .ToList();

        for (var i = 0; i < sorted.Count; i++)
        {
            var currentIndex = ManualPlaylists.IndexOf(sorted[i]);
            if (currentIndex != i)
            {
                ManualPlaylists.Move(currentIndex, i);
            }
        }
    }

    private static string BuildImportTooltip(IReadOnlyList<IPlaylistFileFormat> formats)
    {
        var accepted = string.Join(", ", formats.SelectMany(f => f.Extensions).Distinct(StringComparer.OrdinalIgnoreCase));
        var hints = string.Join(
            Environment.NewLine,
            formats.Select(f => $"\u2022 {f.DisplayName} \u2014 {f.RecommendedUseCase}"));
        return $"Import a playlist ({accepted})." + Environment.NewLine + hints;
    }

    private static string BuildSafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        sanitized = sanitized.Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "Playlist" : sanitized;
    }

    public sealed partial class PlaylistSidebarItem : ObservableObject
    {
        public PlaylistSidebarItem(int? playlistId, string name)
        {
            PlaylistId = playlistId;
            _name = name;
        }

        public int? PlaylistId { get; }
        public bool IsDefault => PlaylistId is null;

        internal string? OriginalName { get; set; }

        [ObservableProperty] private string _name;
        [ObservableProperty] private bool _isPinned;
        [ObservableProperty] private bool _isActive;
        [ObservableProperty] private bool _isRenaming;
        [ObservableProperty] private bool _isSelected;
    }

    /// <summary>
    /// Describes a format option rendered inside the Export submenu.
    /// </summary>
    public sealed class PlaylistExportFormatOption
    {
        public PlaylistExportFormatOption(IPlaylistFileFormat format)
        {
            Format = format;
        }

        public IPlaylistFileFormat Format { get; }
        public string DisplayName => Format.DisplayName;
        public string Tooltip => Format.RecommendedUseCase;
    }

    /// <summary>
    /// Parameter passed from the Export submenu bridging <see cref="PlaylistSidebarItem"/> and the chosen format.
    /// </summary>
    public sealed record PlaylistExportRequest(PlaylistSidebarItem? Item, IPlaylistFileFormat? Format);
}
