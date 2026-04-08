using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.Enums;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;

public partial class MainShellDefaultHomeViewModel : ViewModelBase
{
    public TrackInfoViewModel TrackInfoViewModel { get; }
    public NowPlayingWaveformViewModel NowPlayingWaveformViewModel { get; }
    public PlaybackControlsViewModel PlaybackControlsViewModel { get; }
    public NowPlayingVolumeViewModel NowPlayingVolumeViewModel { get; }
    public MainHomeContentToggleViewModel MainHomeContentToggleViewModel { get; }
    public PlaylistPaneViewModel PlaylistPaneViewModel { get; }
    public SearchResultsPaneViewModel SearchResultsPaneViewModel { get; }
    public SearchbarViewModel SearchbarViewModel { get; }

    private readonly Dictionary<MainHomeContentTarget, ViewModelBase> _contentTargets;

    public ViewModelBase CurrentContentViewModel { get; private set; }

    public MainShellDefaultHomeViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        TrackInfoViewModel trackInfoViewModel,
        NowPlayingWaveformViewModel nowPlayingWaveformViewModel,
        PlaybackControlsViewModel playbackControlsViewModel,
        NowPlayingVolumeViewModel nowPlayingVolumeViewModel,
        MainHomeContentToggleViewModel mainHomeContentToggleViewModel,
        PlaylistPaneViewModel playlistPaneViewModel,
        SearchResultsPaneViewModel searchResultsPaneViewModel,
        SearchbarViewModel searchbarViewModel) : base(errorHandler, logger, messenger)
    {
        TrackInfoViewModel = trackInfoViewModel;
        NowPlayingWaveformViewModel = nowPlayingWaveformViewModel;
        PlaybackControlsViewModel = playbackControlsViewModel;
        NowPlayingVolumeViewModel = nowPlayingVolumeViewModel;
        MainHomeContentToggleViewModel = mainHomeContentToggleViewModel;
        PlaylistPaneViewModel = playlistPaneViewModel;
        SearchResultsPaneViewModel = searchResultsPaneViewModel;
        SearchbarViewModel = searchbarViewModel;

        _contentTargets = new Dictionary<MainHomeContentTarget, ViewModelBase>
        {
            [MainHomeContentTarget.Playlist] = PlaylistPaneViewModel,
            [MainHomeContentTarget.SearchResults] = SearchResultsPaneViewModel
        };

        CurrentContentViewModel = PlaylistPaneViewModel;
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RegisterMessage<QuickSearchExecutedMessage>(OnQuickSearchExecuted);
        RegisterMessage<PlaylistSidebarSelectionChangedMessage>(OnPlaylistSidebarSelectionChanged);
        RegisterMessage<MainHomeContentToggleRequestedMessage>(OnMainHomeContentToggleRequested);

        await TrackInfoViewModel.EnsureInitializedAsync(cancellationToken);
        await NowPlayingWaveformViewModel.EnsureInitializedAsync(cancellationToken);
        await PlaybackControlsViewModel.EnsureInitializedAsync(cancellationToken);
        await NowPlayingVolumeViewModel.EnsureInitializedAsync(cancellationToken);
        await PlaylistPaneViewModel.EnsureInitializedAsync(cancellationToken);
        await SearchResultsPaneViewModel.EnsureInitializedAsync(cancellationToken);
        await SearchbarViewModel.EnsureInitializedAsync(cancellationToken);
        await MainHomeContentToggleViewModel.EnsureInitializedAsync(cancellationToken);

        SetActiveContent(MainHomeContentTarget.Playlist);
    }

    private void OnQuickSearchExecuted(QuickSearchExecutedMessage message)
    {
        SetActiveContent(MainHomeContentTarget.SearchResults);
    }

    private void OnPlaylistSidebarSelectionChanged(PlaylistSidebarSelectionChangedMessage message)
    {
        SetActiveContent(MainHomeContentTarget.Playlist);
    }

    private void OnMainHomeContentToggleRequested(MainHomeContentToggleRequestedMessage message)
    {
        SetActiveContent(message.Value);
    }

    [RelayCommand]
    private void FocusSearchBar()
    {
        Messenger.Send(new FocusSearchBarRequestedMessage());
    }

    [RelayCommand]
    private async Task OpenAdvancedSearch()
    {
        await SearchbarViewModel.OpenAdvancedSearchWindowCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void CreatePlaylist()
    {
        SetActiveContent(MainHomeContentTarget.Playlist);
        PlaylistPaneViewModel.PlaylistSidebarViewModel.CreatePlaylistCommand.Execute(null);
    }

    private void SetActiveContent(MainHomeContentTarget target)
    {
        if (!_contentTargets.TryGetValue(target, out var content))
        {
            return;
        }

        CurrentContentViewModel = content;
        OnPropertyChanged(nameof(CurrentContentViewModel));
        Messenger.Send(new MainHomeContentActiveChangedMessage(target));
    }
}
