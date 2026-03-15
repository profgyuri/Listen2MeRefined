using System.Collections;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.ContextMenus;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using MediatR;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels.ContextMenus;

public class SongContextMenuViewModelTests
{
    [Fact]
    public async Task HandleOpenedAsync_SearchHostSelection_PopulatesPlaylists()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var contextMenuService = new Mock<IPlaylistMembership>();
        var selectionService = new Mock<ISongContextSelectionService>();
        selectionService
            .Setup(x => x.ResolveSearchSelectionPaths(It.IsAny<IEnumerable<AudioModel>>(), It.IsAny<IEnumerable<AudioModel>>()))
            .Returns(["a.mp3"]);
        contextMenuService
            .Setup(x => x.GetPlaylistMembershipInfoAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlaylistMembershipInfo(12, "Workout", true)]);

        var songContextMenuVm = new SongContextMenuViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            contextMenuService.Object,
            selectionService.Object);

        var lists = CreateListsViewModel();
        var searchVm = new SearchResultsPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            lists,
            CreateSettingsReader(),
            Mock.Of<ISearchResultsTransferService>(),
            songContextMenuVm);

        searchVm.SearchResultsSelectionAddedCommand.Execute(new ArrayList
        {
            new AudioModel { Path = "a.mp3", Title = "Track A" }
        });

        await searchVm.InitializeAsync();
        await songContextMenuVm.HandleOpenedAsync();

        Assert.Single(songContextMenuVm.Playlists);
        Assert.Equal("Workout", songContextMenuVm.Playlists[0].PlaylistName);
        contextMenuService.Verify(
            x => x.GetPlaylistMembershipInfoAsync(
                It.Is<IReadOnlyList<string>>(paths => paths.SequenceEqual(new[] { "a.mp3" })),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddToNewPlaylistAsync_ValidName_DelegatesToContextMenuService()
    {
        var logger = CreateLogger();
        var messenger = new WeakReferenceMessenger();
        var contextMenuService = new Mock<IPlaylistMembership>();
        var selectionService = new Mock<ISongContextSelectionService>();
        selectionService
            .Setup(x => x.ResolveSearchSelectionPaths(It.IsAny<IEnumerable<AudioModel>>(), It.IsAny<IEnumerable<AudioModel>>()))
            .Returns(["a.mp3", "b.mp3"]);
        contextMenuService
            .Setup(x => x.GetPlaylistMembershipInfoAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlaylistMembershipInfo>());

        var songContextMenuVm = new SongContextMenuViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            contextMenuService.Object,
            selectionService.Object);

        var lists = CreateListsViewModel();
        var searchVm = new SearchResultsPaneViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            lists,
            CreateSettingsReader(),
            Mock.Of<ISearchResultsTransferService>(),
            songContextMenuVm);

        await searchVm.InitializeAsync();
        await songContextMenuVm.AddToNewPlaylistAsync("Fresh");

        contextMenuService.Verify(
            x => x.AddToNewPlaylistAsync(
                "Fresh",
                It.Is<IReadOnlyList<string>>(paths => paths.Contains("a.mp3") && paths.Contains("b.mp3")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Mock<ILogger> CreateLogger()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        return logger;
    }

    private static ListsViewModel CreateListsViewModel()
    {
        var logger = CreateLogger();
        var mediator = new Mock<IMediator>();
        var audioSearchExecutionService = new Mock<IAudioSearchExecutionService>();
        var scanner = new Mock<IFileScanner>();
        var settingsReader = new Mock<IAppSettingsReader>();
        var playerController = new Mock<IMusicPlayerController>();
        var playlist = new PlaylistQueue();
        settingsReader.Setup(x => x.GetMusicFolders()).Returns(Array.Empty<string>());
        settingsReader.Setup(x => x.GetMutedDroppedSongFolders()).Returns(Array.Empty<string>());
        var settingsWriter = new Mock<IAppSettingsWriter>();
        var prompt = new Mock<IDroppedSongFolderPromptService>();
        prompt.Setup(x => x.PromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddDroppedSongFolderDecision.Skip);
        var externalAudioOpenService = new Mock<IExternalAudioOpenService>();
        settingsReader
            .Setup(x => x.GetSearchResultsTransferMode())
            .Returns(SearchResultsTransferMode.Move);
        var ui = new Mock<IUiDispatcher>();

        return new ListsViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            new WeakReferenceMessenger(),
            mediator.Object,
            audioSearchExecutionService.Object,
            scanner.Object,
            settingsReader.Object,
            playerController.Object,
            playlist,
            settingsWriter.Object,
            prompt.Object,
            externalAudioOpenService.Object,
            ui.Object);
    }

    private static IAppSettingsReader CreateSettingsReader()
    {
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetMusicFolders()).Returns(Array.Empty<string>());
        settingsReader.Setup(x => x.GetMutedDroppedSongFolders()).Returns(Array.Empty<string>());
        settingsReader
            .Setup(x => x.GetSearchResultsTransferMode())
            .Returns(SearchResultsTransferMode.Move);
        return settingsReader.Object;
    }
}
