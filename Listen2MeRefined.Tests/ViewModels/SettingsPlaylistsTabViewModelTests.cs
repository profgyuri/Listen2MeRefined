using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.ViewModels.SettingsTabs;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class SettingsPlaylistsTabViewModelTests
{
    [Fact]
    public async Task InitializeAsync_State_LoadsPersistedPlaylistSettings()
    {
        var settings = new AppSettings
        {
            FontFamily = "Consolas",
            SearchResultsTransferMode = SearchResultsTransferMode.Copy
        };

        var seedPlaylists = new[]
        {
            new PlaylistSummary(10, "Favorites"),
            new PlaylistSummary(20, "Road Trip")
        };

        var (viewModel, _, _, _) = CreateViewModel(settings, seedPlaylists);
        await viewModel.InitializeAsync();

        Assert.Equal("Consolas", viewModel.FontFamilyName);
        Assert.Equal(SearchResultsTransferMode.Copy, viewModel.SelectedSearchResultsTransferMode);
        Assert.Equal(2, viewModel.Playlists.Count);
        Assert.Equal(10, viewModel.SelectedPlaylist?.Id);
        Assert.Equal("Favorites", viewModel.PlaylistNameInput);
    }

    [Fact]
    public async Task SelectedSearchResultsTransferMode_State_PersistsSetting()
    {
        var settings = new AppSettings
        {
            SearchResultsTransferMode = SearchResultsTransferMode.Move
        };

        var (viewModel, _, _, _) = CreateViewModel(settings, []);
        await viewModel.InitializeAsync();

        viewModel.SelectedSearchResultsTransferMode = SearchResultsTransferMode.Copy;

        Assert.Equal(SearchResultsTransferMode.Copy, settings.SearchResultsTransferMode);
    }

    [Fact]
    public async Task CreatePlaylistCommand_State_CreatesAndPublishesMessage()
    {
        var settings = new AppSettings();
        var (viewModel, _, _, probe) = CreateViewModel(
            settings,
            [new PlaylistSummary(1, "Existing")]);
        await viewModel.InitializeAsync();

        viewModel.PlaylistNameInput = "Fresh";
        await viewModel.CreatePlaylistCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Playlists.Count);
        Assert.Equal("Fresh", viewModel.SelectedPlaylist?.Name);
        Assert.Equal(string.Empty, viewModel.PlaylistNameInput);
        Assert.NotNull(probe.Created);
        Assert.Equal("Fresh", probe.Created?.Name);
    }

    [Fact]
    public async Task RenameSelectedPlaylistCommand_State_RenamesAndPublishesMessage()
    {
        var settings = new AppSettings();
        var (viewModel, _, _, probe) = CreateViewModel(
            settings,
            [new PlaylistSummary(42, "Old Name")]);
        await viewModel.InitializeAsync();

        viewModel.SelectedPlaylist = viewModel.Playlists[0];
        viewModel.PlaylistNameInput = "New Name";
        await viewModel.RenameSelectedPlaylistCommand.ExecuteAsync(null);

        Assert.Equal("New Name", viewModel.SelectedPlaylist?.Name);
        Assert.NotNull(probe.Renamed);
        Assert.Equal(42, probe.Renamed?.PlaylistId);
        Assert.Equal("New Name", probe.Renamed?.Name);
    }

    [Fact]
    public async Task DeleteSelectedPlaylistCommand_State_DeletesAndPublishesMessage()
    {
        var settings = new AppSettings();
        var (viewModel, _, _, probe) = CreateViewModel(
            settings,
            [
                new PlaylistSummary(10, "A"),
                new PlaylistSummary(20, "B")
            ]);
        await viewModel.InitializeAsync();

        viewModel.SelectedPlaylist = viewModel.Playlists[0];
        await viewModel.DeleteSelectedPlaylistCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Playlists);
        Assert.Equal(20, viewModel.Playlists[0].Id);
        Assert.Equal(20, viewModel.SelectedPlaylist?.Id);
        Assert.NotNull(probe.Deleted);
        Assert.Equal(10, probe.Deleted?.PlaylistId);
    }

    [Fact]
    public async Task CreatePlaylistCommand_WhenServiceThrows_UsesErrorHandler()
    {
        var settings = new AppSettings();
        var (viewModel, _, errorHandler, _) = CreateViewModel(
            settings,
            [],
            throwOnCreate: true);
        await viewModel.InitializeAsync();

        viewModel.PlaylistNameInput = "Failure";
        await viewModel.CreatePlaylistCommand.ExecuteAsync(null);

        errorHandler.Verify(
            x => x.HandleAsync(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static (
        SettingsPlaylistsTabViewModel ViewModel,
        Mock<IPlaylistLibraryService> PlaylistLibraryService,
        Mock<IErrorHandler> ErrorHandler,
        MessageProbe Probe) CreateViewModel(
            AppSettings settings,
            IReadOnlyList<PlaylistSummary> seedPlaylists,
            bool throwOnCreate = false)
    {
        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);
        settingsManager
            .Setup(x => x.SaveSettings(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(apply => apply(settings));

        var playlists = seedPlaylists
            .Select(x => new PlaylistSummary(x.Id, x.Name))
            .ToList();
        var nextPlaylistId = playlists.Count == 0
            ? 1
            : playlists.Max(x => x.Id) + 1;

        var playlistLibraryService = new Mock<IPlaylistLibraryService>();
        playlistLibraryService
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => playlists.Select(x => new PlaylistSummary(x.Id, x.Name)).ToArray());

        if (throwOnCreate)
        {
            playlistLibraryService
                .Setup(x => x.CreatePlaylistAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Create failure"));
        }
        else
        {
            playlistLibraryService
                .Setup(x => x.CreatePlaylistAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string name, CancellationToken _) =>
                {
                    var created = new PlaylistSummary(nextPlaylistId++, name.Trim());
                    playlists.Add(created);
                    return created;
                });
        }

        playlistLibraryService
            .Setup(x => x.RenamePlaylistAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((int playlistId, string newName, CancellationToken _) =>
            {
                var index = playlists.FindIndex(x => x.Id == playlistId);
                if (index >= 0)
                {
                    playlists[index] = new PlaylistSummary(playlistId, newName.Trim());
                }

                return Task.CompletedTask;
            });

        playlistLibraryService
            .Setup(x => x.DeletePlaylistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((int playlistId, CancellationToken _) =>
            {
                playlists.RemoveAll(x => x.Id == playlistId);
                return Task.CompletedTask;
            });

        var errorHandler = new Mock<IErrorHandler>();
        errorHandler
            .Setup(x => x.HandleAsync(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        var messenger = new WeakReferenceMessenger();
        var probe = new MessageProbe();
        messenger.Register<MessageProbe, PlaylistCreatedMessage>(
            probe,
            static (recipient, message) => recipient.Created = message.Value);
        messenger.Register<MessageProbe, PlaylistRenamedMessage>(
            probe,
            static (recipient, message) => recipient.Renamed = message.Value);
        messenger.Register<MessageProbe, PlaylistDeletedMessage>(
            probe,
            static (recipient, message) => recipient.Deleted = message.Value);

        var viewModel = new SettingsPlaylistsTabViewModel(
            errorHandler.Object,
            logger.Object,
            messenger,
            new AppSettingsReader(settingsManager.Object),
            new AppSettingsWriter(settingsManager.Object),
            playlistLibraryService.Object);

        return (viewModel, playlistLibraryService, errorHandler, probe);
    }

    private sealed class MessageProbe
    {
        public PlaylistCreatedMessageData? Created { get; set; }

        public PlaylistRenamedMessageData? Renamed { get; set; }

        public PlaylistDeletedMessageData? Deleted { get; set; }
    }
}
