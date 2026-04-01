using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.ContextMenus;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class MainShellDefaultHomeViewModelTests
{
    [Fact]
    public async Task InitializeAsync_State_PublishesInitialPlaylistActiveMessage()
    {
        var (viewModel, messenger, _, playlistPaneViewModel, _, _, _, _, _) = CreateViewModel();
        var publishedTargets = RegisterActiveChangedProbe(messenger);

        await viewModel.InitializeAsync();

        Assert.Same(playlistPaneViewModel, viewModel.CurrentContentViewModel);
        Assert.Contains(MainHomeContentTarget.Playlist, publishedTargets);
    }

    [Fact]
    public async Task ToggleRequestedMessage_State_SwitchesToRequestedContentAndPublishes()
    {
        var (viewModel, messenger, _, _, searchResultsPaneViewModel, _, _, _, _) = CreateViewModel();
        var publishedTargets = RegisterActiveChangedProbe(messenger);
        await viewModel.InitializeAsync();
        publishedTargets.Clear();

        messenger.Send(new MainHomeContentToggleRequestedMessage(MainHomeContentTarget.SearchResults));

        Assert.Same(searchResultsPaneViewModel, viewModel.CurrentContentViewModel);
        Assert.Contains(MainHomeContentTarget.SearchResults, publishedTargets);
    }

    [Fact]
    public async Task QuickSearchExecutedMessage_State_SwitchesToSearchResultsAndPublishes()
    {
        var (viewModel, messenger, _, _, searchResultsPaneViewModel, _, _, _, _) = CreateViewModel();
        var publishedTargets = RegisterActiveChangedProbe(messenger);
        await viewModel.InitializeAsync();
        publishedTargets.Clear();

        messenger.Send(new QuickSearchExecutedMessage([]));

        Assert.Same(searchResultsPaneViewModel, viewModel.CurrentContentViewModel);
        Assert.Contains(MainHomeContentTarget.SearchResults, publishedTargets);
    }

    [Fact]
    public async Task PlaylistSidebarSelectionChangedMessage_State_SwitchesToPlaylistAndPublishes()
    {
        var (viewModel, messenger, _, playlistPaneViewModel, _, _, _, _, _) = CreateViewModel();
        var publishedTargets = RegisterActiveChangedProbe(messenger);
        await viewModel.InitializeAsync();
        publishedTargets.Clear();
        messenger.Send(new MainHomeContentToggleRequestedMessage(MainHomeContentTarget.SearchResults));
        publishedTargets.Clear();

        messenger.Send(new PlaylistSidebarSelectionChangedMessage(new PlaylistSidebarSelectionData(42)));

        Assert.Same(playlistPaneViewModel, viewModel.CurrentContentViewModel);
        Assert.Contains(MainHomeContentTarget.Playlist, publishedTargets);
    }

    private static List<MainHomeContentTarget> RegisterActiveChangedProbe(IMessenger messenger)
    {
        var recipient = new object();
        var publishedTargets = new List<MainHomeContentTarget>();
        messenger.Register<MainHomeContentActiveChangedMessage>(recipient, (_, message) => publishedTargets.Add(message.Value));
        return publishedTargets;
    }

    private static (
        MainShellDefaultHomeViewModel ViewModel,
        WeakReferenceMessenger Messenger,
        TrackInfoViewModel TrackInfoViewModel,
        PlaylistPaneViewModel PlaylistPaneViewModel,
        SearchResultsPaneViewModel SearchResultsPaneViewModel,
        SearchbarViewModel SearchbarViewModel,
        PlaybackControlsViewModel PlaybackControlsViewModel,
        NowPlayingWaveformViewModel NowPlayingWaveformViewModel,
        NowPlayingVolumeViewModel NowPlayingVolumeViewModel) CreateViewModel()
    {
        var messenger = new WeakReferenceMessenger();
        var logger = CreateLogger();
        var queueState = CreateQueueState();

        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetFontFamily()).Returns("Segoe UI");
        settingsReader.Setup(x => x.GetAutoFlowTrackText()).Returns(false);

        var musicPlayer = new Mock<IMusicPlayerController>();
        musicPlayer.SetupProperty(x => x.CurrentTime, 0d);
        musicPlayer.SetupProperty(x => x.Volume, 1f);
        musicPlayer.SetupProperty(x => x.RepeatMode, RepeatMode.Off);

        var trackInfoViewModel = new StubTrackInfoViewModel(
            logger.Object,
            messenger,
            settingsReader.Object);

        var nowPlayingWaveformViewModel = new StubNowPlayingWaveformViewModel(
            logger.Object,
            messenger,
            musicPlayer.Object);

        var playbackControlsViewModel = new StubPlaybackControlsViewModel(
            logger.Object,
            messenger,
            musicPlayer.Object,
            settingsReader.Object);

        var nowPlayingVolumeViewModel = new StubNowPlayingVolumeViewModel(
            logger.Object,
            messenger,
            musicPlayer.Object);

        var playlistSidebarViewModel = new PlaylistSidebarViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            Mock.Of<IPlaylistLibraryService>(),
            queueState.Object);

        var playlistSongContextMenuViewModel = new SongContextMenuViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            Mock.Of<IPlaylistMembership>(),
            Mock.Of<ISongContextSelectionService>());

        var playlistPaneViewModel = new StubPlaylistPaneViewModel(
            logger.Object,
            messenger,
            queueState.Object,
            settingsReader.Object,
            playlistSidebarViewModel,
            playlistSongContextMenuViewModel);

        var searchSongContextMenuViewModel = new SongContextMenuViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            Mock.Of<IPlaylistMembership>(),
            Mock.Of<ISongContextSelectionService>());

        var searchResultsPaneViewModel = new StubSearchResultsPaneViewModel(
            logger.Object,
            messenger,
            queueState.Object,
            settingsReader.Object,
            searchSongContextMenuViewModel);

        var searchbarViewModel = new StubSearchbarViewModel(
            logger.Object,
            messenger,
            settingsReader.Object);

        var mainHomeContentToggleViewModel = new MainHomeContentToggleViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger);

        var viewModel = new MainShellDefaultHomeViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            trackInfoViewModel,
            nowPlayingWaveformViewModel,
            playbackControlsViewModel,
            nowPlayingVolumeViewModel,
            mainHomeContentToggleViewModel,
            playlistPaneViewModel,
            searchResultsPaneViewModel,
            searchbarViewModel);

        return (
            viewModel,
            messenger,
            trackInfoViewModel,
            playlistPaneViewModel,
            searchResultsPaneViewModel,
            searchbarViewModel,
            playbackControlsViewModel,
            nowPlayingWaveformViewModel,
            nowPlayingVolumeViewModel);
    }

    private static Mock<IPlaylistQueueState> CreateQueueState()
    {
        var queueState = new Mock<IPlaylistQueueState>();
        queueState.SetupGet(x => x.PlayList).Returns(new ObservableCollection<AudioModel>());
        queueState.SetupGet(x => x.DefaultPlaylist).Returns(new ObservableCollection<AudioModel>());
        queueState.SetupProperty(x => x.SelectedSong);
        queueState.SetupProperty(x => x.SelectedIndex, -1);
        queueState.SetupProperty(x => x.CurrentSongIndex, -1);
        queueState.SetupGet(x => x.ActiveNamedPlaylistId).Returns((int?)null);
        queueState.SetupGet(x => x.IsDefaultPlaylistActive).Returns(true);
        return queueState;
    }

    private static Mock<ILogger> CreateLogger()
    {
        var logger = new Mock<ILogger>();
        logger.Setup(x => x.ForContext(It.IsAny<Type>())).Returns(logger.Object);
        return logger;
    }

    private sealed class StubTrackInfoViewModel(
        ILogger logger,
        IMessenger messenger,
        IAppSettingsReader settingsReader)
        : TrackInfoViewModel(Mock.Of<IErrorHandler>(), logger, messenger, settingsReader)
    {
        public override Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubNowPlayingWaveformViewModel(
        ILogger logger,
        IMessenger messenger,
        IMusicPlayerController musicPlayerController)
        : NowPlayingWaveformViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            Mock.Of<IWaveformRenderer>(),
            Mock.Of<IWaveformViewportPolicy>(),
            Mock.Of<IWaveformResizeScheduler>(),
            musicPlayerController,
            new ImmediateUiDispatcher(),
            new TimedTask())
    {
        public override Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubPlaybackControlsViewModel(
        ILogger logger,
        IMessenger messenger,
        IMusicPlayerController musicPlayerController,
        IAppSettingsReader settingsReader)
        : PlaybackControlsViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            musicPlayerController,
            settingsReader,
            new ImmediateUiDispatcher(),
            new TimedTask())
    {
        public override Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubNowPlayingVolumeViewModel(
        ILogger logger,
        IMessenger messenger,
        IMusicPlayerController musicPlayerController)
        : NowPlayingVolumeViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            Mock.Of<IPlaybackVolumeSetter>(),
            musicPlayerController)
    {
        public override Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubPlaylistPaneViewModel(
        ILogger logger,
        IMessenger messenger,
        IPlaylistQueueState queueState,
        IAppSettingsReader settingsReader,
        PlaylistSidebarViewModel playlistSidebarViewModel,
        SongContextMenuViewModel songContextMenuViewModel)
        : PlaylistPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            queueState,
            Mock.Of<IPlaylistQueueRoutingService>(),
            Mock.Of<IDefaultPlaylistService>(),
            Mock.Of<IPlaybackQueueActionsService>(),
            Mock.Of<IExternalDropImportService>(),
            Mock.Of<IPlaylistSelectionService>(),
            Mock.Of<IPlaylistLibraryService>(),
            Mock.Of<IPlaybackContextSyncService>(),
            Mock.Of<IExternalAudioOpenService>(),
            Mock.Of<IExternalAudioOpenInbox>(),
            settingsReader,
            playlistSidebarViewModel,
            songContextMenuViewModel)
    {
        public override Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubSearchResultsPaneViewModel(
        ILogger logger,
        IMessenger messenger,
        IPlaylistQueueState queueState,
        IAppSettingsReader settingsReader,
        SongContextMenuViewModel songContextMenuViewModel)
        : SearchResultsPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            queueState,
            settingsReader,
            Mock.Of<IAudioSearchExecutionService>(),
            Mock.Of<ISearchResultsTransferService>(),
            songContextMenuViewModel)
    {
        public override Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubSearchbarViewModel(
        ILogger logger,
        IMessenger messenger,
        IAppSettingsReader settingsReader)
        : SearchbarViewModel(
            Mock.Of<IErrorHandler>(),
            logger,
            messenger,
            Mock.Of<IAudioSearchExecutionService>(),
            settingsReader,
            Mock.Of<IWindowManager>())
    {
        public override Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;

        public Task InvokeAsync(Action action, CancellationToken ct = default)
        {
            action();
            return Task.CompletedTask;
        }

        public Task InvokeAsync(Func<Task> func, CancellationToken ct = default) => func();

        public Task<T> InvokeAsync<T>(Func<T> func, CancellationToken ct = default) => Task.FromResult(func());
    }
}
