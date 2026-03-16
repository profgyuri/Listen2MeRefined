using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.Models;
using MediatR;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class ListsViewModel :
    ViewModelBase,
    INotificationHandler<CurrentSongNotification>,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<AdvancedSearchNotification>,
    INotificationHandler<ExternalAudioFilesOpenedNotification>
{
    private readonly IMediator _mediator;
    private readonly IAudioSearchExecutionService _audioSearchExecutionService;
    private readonly IPlaylistQueueState _playlistQueueState;
    private readonly IPlaylistQueueRoutingService _playlistQueueRoutingService;
    private readonly IExternalDropImportService _externalDropImportService;
    private readonly IPlaybackContextSyncService _playbackContextSyncService;
    private readonly IExternalAudioOpenService _externalAudioOpenService;

    [ObservableProperty] private string? _fontFamilyName = string.Empty;

    public ObservableCollection<AudioModel> PlayList => _playlistQueueState.PlayList;
    public ObservableCollection<AudioModel> DefaultPlaylist => _playlistQueueState.DefaultPlaylist;
    public int? ActiveNamedPlaylistId => _playlistQueueState.ActiveNamedPlaylistId;
    public bool IsDefaultPlaylistActive => _playlistQueueState.IsDefaultPlaylistActive;

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

    public ListsViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IMediator mediator,
        IAudioSearchExecutionService audioSearchExecutionService,
        IPlaylistQueueState playlistQueueState,
        IPlaylistQueueRoutingService playlistQueueRoutingService,
        IExternalDropImportService externalDropImportService,
        IPlaybackContextSyncService playbackContextSyncService,
        IExternalAudioOpenService externalAudioOpenService) : base(errorHandler, logger, messenger)
    {
        _mediator = mediator;
        _audioSearchExecutionService = audioSearchExecutionService;
        _playlistQueueState = playlistQueueState;
        _playlistQueueRoutingService = playlistQueueRoutingService;
        _externalDropImportService = externalDropImportService;
        _playbackContextSyncService = playbackContextSyncService;
        _externalAudioOpenService = externalAudioOpenService;
        _playlistQueueState.PropertyChanged += PlaylistQueueStateOnPropertyChanged;

        Logger.Debug("[ListsViewModel] Class initialized");
    }

    public Task HandleExternalFileDropAsync(IReadOnlyList<string> droppedPaths, int insertIndex, CancellationToken ct = default) =>
        _externalDropImportService.HandleExternalFileDropAsync(droppedPaths, insertIndex, ct);

    public void ActivateNamedPlaylistQueue(int playlistId, IEnumerable<AudioModel> songs)
    {
        _playlistQueueRoutingService.ActivateNamedPlaylistQueue(playlistId, songs);
    }

    public Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        Logger.Information("[ListsViewModel] Font family changed to {FontFamily}", notification.FontFamily);
        FontFamilyName = notification.FontFamily;
        return Task.CompletedTask;
    }

    public Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        Logger.Information("[ListsViewModel] Current song changed to {@Audio}", notification.Audio);
        _externalAudioOpenService.SetCurrentSong(notification.Audio);
        _playbackContextSyncService.SetCurrentSong(notification.Audio);
        return Task.CompletedTask;
    }

    public Task Handle(ExternalAudioFilesOpenedNotification notification, CancellationToken cancellationToken)
    {
        Logger.Information("[ListsViewModel] Handling {Count} shell-opened audio file(s)", notification.Paths.Count);
        return _externalAudioOpenService.OpenAsync(notification.Paths, cancellationToken);
    }

    public async Task Handle(AdvancedSearchNotification notification, CancellationToken cancellationToken)
    {
        Logger.Information("[ListsViewModel] Performing advanced search with {@Filters} filters (MatchMode: {MatchMode})",
            notification.Filters, notification.MatchMode);
        var result =
            (await _audioSearchExecutionService.ExecuteAdvancedSearchAsync(notification.Filters, notification.MatchMode)).ToArray();

        Logger.Information("[ListsViewModel] Advanced search returned {Count} results", result.Length);
        if (result.Length > 0)
        {
            Logger.Verbose(
                "[ListsViewModel] First {Shown} results are: {@Results}",
                Math.Min(5, result.Length),
                result.Take(5));
        }

        Messenger.Send(new SearchResultsUpdatedMessage(result));
        await _mediator.Publish(new AdvancedSearchCompletedNotification(result.Length), cancellationToken);
    }

    private void PlaylistQueueStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IPlaylistQueueState.SelectedSong))
        {
            OnPropertyChanged(nameof(SelectedSong));
        }
        else if (e.PropertyName == nameof(IPlaylistQueueState.SelectedIndex))
        {
            OnPropertyChanged(nameof(SelectedIndex));
        }
        else if (e.PropertyName == nameof(IPlaylistQueueState.ActiveNamedPlaylistId))
        {
            OnPropertyChanged(nameof(ActiveNamedPlaylistId));
            OnPropertyChanged(nameof(IsDefaultPlaylistActive));
        }
    }
}
