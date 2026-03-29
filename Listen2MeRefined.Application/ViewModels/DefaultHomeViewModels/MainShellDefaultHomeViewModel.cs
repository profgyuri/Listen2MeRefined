using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;

public partial class MainShellDefaultHomeViewModel : ViewModelBase
{
    public TrackInfoViewModel TrackInfoViewModel { get; }
    public PlaybackControlsViewModel PlaybackControlsViewModel { get; }
    public PlaylistPaneViewModel PlaylistPaneViewModel { get; }
    public SearchResultsPaneViewModel SearchResultsPaneViewModel { get; }
    public SearchbarViewModel SearchbarViewModel { get; }

    [ObservableProperty] private ViewModelBase _currentContentViewModel;
    [ObservableProperty] private bool _isPlaylistViewActive = true;

    public MainShellDefaultHomeViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        TrackInfoViewModel trackInfoViewModel,
        PlaybackControlsViewModel playbackControlsViewModel,
        PlaylistPaneViewModel playlistPaneViewModel,
        SearchResultsPaneViewModel searchResultsPaneViewModel,
        SearchbarViewModel searchbarViewModel) : base(errorHandler, logger, messenger)
    {
        TrackInfoViewModel = trackInfoViewModel;
        PlaybackControlsViewModel = playbackControlsViewModel;
        PlaylistPaneViewModel = playlistPaneViewModel;
        SearchResultsPaneViewModel = searchResultsPaneViewModel;
        SearchbarViewModel = searchbarViewModel;
        _currentContentViewModel = playlistPaneViewModel;
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RegisterMessage<QuickSearchExecutedMessage>(OnQuickSearchExecuted);
        RegisterMessage<PlaylistSidebarSelectionChangedMessage>(OnPlaylistSidebarSelectionChanged);

        await TrackInfoViewModel.EnsureInitializedAsync(cancellationToken);
        await PlaybackControlsViewModel.EnsureInitializedAsync(cancellationToken);
        await PlaylistPaneViewModel.EnsureInitializedAsync(cancellationToken);
        await SearchResultsPaneViewModel.EnsureInitializedAsync(cancellationToken);
        await SearchbarViewModel.EnsureInitializedAsync(cancellationToken);
    }

    [RelayCommand]
    private void ShowPlaylistView()
    {
        CurrentContentViewModel = PlaylistPaneViewModel;
        IsPlaylistViewActive = true;
    }

    [RelayCommand]
    private void ShowSearchResultsView()
    {
        CurrentContentViewModel = SearchResultsPaneViewModel;
        IsPlaylistViewActive = false;
    }

    private void OnQuickSearchExecuted(QuickSearchExecutedMessage message)
    {
        CurrentContentViewModel = SearchResultsPaneViewModel;
        IsPlaylistViewActive = false;
    }

    private void OnPlaylistSidebarSelectionChanged(PlaylistSidebarSelectionChangedMessage message)
    {
        CurrentContentViewModel = PlaylistPaneViewModel;
        IsPlaylistViewActive = true;
    }
}