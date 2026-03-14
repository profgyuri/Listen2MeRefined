using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;

public class MainShellDefaultHomeViewModel : ViewModelBase
{
    public TrackInfoViewModel TrackInfoViewModel { get; }
    public PlaybackControlsViewModel PlaybackControlsViewModel { get; }
    public PlaylistPaneViewModel PlaylistPaneViewModel { get; }
    public SearchResultsPaneViewModel SearchResultsPaneViewModel { get; }
    public SearchbarViewModel SearchbarViewModel { get; }
    
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
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await TrackInfoViewModel.EnsureInitializedAsync(cancellationToken);
        await PlaybackControlsViewModel.EnsureInitializedAsync(cancellationToken);
        await PlaylistPaneViewModel.EnsureInitializedAsync(cancellationToken);
        await SearchResultsPaneViewModel.EnsureInitializedAsync(cancellationToken);
        await SearchbarViewModel.EnsureInitializedAsync(cancellationToken);
    }
}