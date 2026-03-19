using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.SettingsTabs;

public partial class SettingsPlaylistsTabViewModel : ViewModelBase
{
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAppSettingsWriter _settingsWriter;
    private readonly IPlaylistLibraryService _playlistLibraryService;
    private bool _isLoadingSettings;

    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private ObservableCollection<PlaylistSummary> _playlists = [];
    [ObservableProperty] private PlaylistSummary? _selectedPlaylist;
    [ObservableProperty] private string _playlistNameInput = string.Empty;
    [ObservableProperty] private SearchResultsTransferMode _selectedSearchResultsTransferMode = SearchResultsTransferMode.Move;

    public ObservableCollection<SearchResultsTransferMode> SearchResultsTransferModes { get; } =
        new(Enum.GetValues<SearchResultsTransferMode>());

    public SettingsPlaylistsTabViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IAppSettingsReader settingsReader,
        IAppSettingsWriter settingsWriter,
        IPlaylistLibraryService playlistLibraryService) : base(errorHandler, logger, messenger)
    {
        _settingsReader = settingsReader;
        _settingsWriter = settingsWriter;
        _playlistLibraryService = playlistLibraryService;
    }

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);

        _isLoadingSettings = true;
        try
        {
            FontFamilyName = _settingsReader.GetFontFamily();
            SelectedSearchResultsTransferMode = _settingsReader.GetSearchResultsTransferMode();
            await ReloadPlaylistsAsync(ct);
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    partial void OnSelectedSearchResultsTransferModeChanged(SearchResultsTransferMode value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetSearchResultsTransferMode(value);
    }

    partial void OnSelectedPlaylistChanged(PlaylistSummary? value)
    {
        PlaylistNameInput = value?.Name ?? string.Empty;
    }

    [RelayCommand]
    private Task CreatePlaylistAsync() =>
        ExecuteSafeAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(PlaylistNameInput))
            {
                return;
            }

            var created = await _playlistLibraryService.CreatePlaylistAsync(PlaylistNameInput.Trim(), ct);
            await ReloadPlaylistsAsync(ct);
            SelectedPlaylist = Playlists.FirstOrDefault(x => x.Id == created.Id);
            PlaylistNameInput = string.Empty;

            Messenger.Send(new PlaylistCreatedMessage(new PlaylistCreatedMessageData(created.Id, created.Name)));
        });

    [RelayCommand]
    private Task RenameSelectedPlaylistAsync() =>
        ExecuteSafeAsync(async ct =>
        {
            if (SelectedPlaylist is null || string.IsNullOrWhiteSpace(PlaylistNameInput))
            {
                return;
            }

            var playlistId = SelectedPlaylist.Id;
            var newName = PlaylistNameInput.Trim();

            await _playlistLibraryService.RenamePlaylistAsync(playlistId, newName, ct);
            await ReloadPlaylistsAsync(ct);
            SelectedPlaylist = Playlists.FirstOrDefault(x => x.Id == playlistId);

            Messenger.Send(new PlaylistRenamedMessage(new PlaylistRenamedMessageData(playlistId, newName)));
        });

    [RelayCommand]
    private Task DeleteSelectedPlaylistAsync() =>
        ExecuteSafeAsync(async ct =>
        {
            if (SelectedPlaylist is null)
            {
                return;
            }

            var playlistId = SelectedPlaylist.Id;
            await _playlistLibraryService.DeletePlaylistAsync(playlistId, ct);
            await ReloadPlaylistsAsync(ct);
            SelectedPlaylist = Playlists.FirstOrDefault();
            PlaylistNameInput = SelectedPlaylist?.Name ?? string.Empty;

            Messenger.Send(new PlaylistDeletedMessage(new PlaylistDeletedMessageData(playlistId)));
        });

    private async Task ReloadPlaylistsAsync(CancellationToken ct = default)
    {
        var previousSelection = SelectedPlaylist;
        var playlists = await _playlistLibraryService.GetAllPlaylistsAsync(ct);
        Playlists = new ObservableCollection<PlaylistSummary>(playlists);

        if (previousSelection is not null)
        {
            SelectedPlaylist = Playlists.FirstOrDefault(x => x.Id == previousSelection.Id);
        }
        else if (Playlists.Count > 0)
        {
            SelectedPlaylist = Playlists[0];
        }

        PlaylistNameInput = SelectedPlaylist?.Name ?? string.Empty;
    }

    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        FontFamilyName = message.Value;
        Logger.Debug("[SettingsPlaylistsTabViewModel] Received FontFamilyChangedMessage: {message}", message.Value);
    }
}
