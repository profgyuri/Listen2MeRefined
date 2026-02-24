using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Mvvm;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.Infrastructure.SystemOperations;
using MediatR;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Mvvm;

public sealed class FolderBrowserViewModelTests
{
    [Fact]
    public async Task InitializeAsync_UsesLastBrowsedFolder_WhenEnabledAndValid()
    {
        var settings = new AppSettings
        {
            FolderBrowserStartAtLastLocation = true,
            LastBrowsedFolder = @"C:\Music"
        };

        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"],
                subFoldersByPath: new Dictionary<string, IReadOnlyList<string>>
                {
                    [@"C:\Music"] = ["Rock", "Jazz"]
                }),
            out _);

        await viewModel.InitializeAsync();

        Assert.Equal(@"C:\Music", viewModel.FullPath);
        Assert.Contains("Rock", viewModel.Folders);
        Assert.Contains("Jazz", viewModel.Folders);
    }

    [Fact]
    public async Task GoToPathCommand_InvalidPath_SetsValidationError()
    {
        var settings = new AppSettings();
        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"]),
            out _);

        await viewModel.InitializeAsync();
        viewModel.FullPath = @"Z:\Missing";

        viewModel.GoToPathCommand.Execute(null);

        Assert.True(viewModel.HasValidationError);
        Assert.Contains("Could not open", viewModel.ValidationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TryHandleSelectedPathAsync_InvalidPath_ReturnsFalse()
    {
        var settings = new AppSettings();
        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"]),
            out var mediator);

        await viewModel.InitializeAsync();
        viewModel.FullPath = @"Z:\Missing";

        var result = await viewModel.TryHandleSelectedPathAsync();

        Assert.False(result);
        Assert.True(viewModel.HasValidationError);
        mediator.Verify(
            x => x.Publish(It.IsAny<FolderBrowserNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TryHandleSelectedPathAsync_ValidPath_PublishesNotification()
    {
        var settings = new AppSettings();

        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"]),
            out var mediator);

        await viewModel.InitializeAsync();
        viewModel.FullPath = @"C:\Music";

        var result = await viewModel.TryHandleSelectedPathAsync();

        Assert.True(result);
        mediator.Verify(
            x => x.Publish(
                It.Is<FolderBrowserNotification>(n => n.Path == @"C:\Music"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TogglePinCommand_PublishesPinnedFoldersChangedNotification()
    {
        var settings = new AppSettings();
        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"]),
            out var mediator);

        await viewModel.InitializeAsync();
        viewModel.FullPath = @"C:\Music";

        await viewModel.TogglePinCommand.ExecuteAsync(null);

        mediator.Verify(
            x => x.Publish(
                It.Is<PinnedFoldersChangedNotification>(n => n.PinnedFolders.Contains(@"C:\Music")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static FolderBrowserViewModel CreateViewModel(
        AppSettings settings,
        IFolderBrowser folderBrowser,
        out Mock<IMediator> mediator)
    {
        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);
        settingsManager
            .Setup(x => x.SaveSettings(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(apply => apply(settings));

        mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Publish(It.IsAny<FolderBrowserNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mediator.Setup(x => x.Publish(It.IsAny<PinnedFoldersChangedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var settingsReadService = new AppSettingsReader(settingsManager.Object);
        var settingsWriteService = new AppSettingsWriter(settingsManager.Object);
        var folderNavigationService = new FolderNavigationService(folderBrowser);
        var pinnedFoldersService = new PinnedFoldersService(folderBrowser);

        return new FolderBrowserViewModel(
            Mock.Of<ILogger>(),
            mediator.Object,
            folderNavigationService,
            pinnedFoldersService,
            settingsReadService,
            settingsWriteService);
    }

    private sealed class FakeFolderBrowser(
        IReadOnlyList<string> drives,
        IReadOnlyCollection<string> existingFolders,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? subFoldersByPath = null) : IFolderBrowser
    {
        private readonly IReadOnlyList<string> _drives = drives;
        private readonly IReadOnlyCollection<string> _existingFolders = existingFolders;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _subFoldersByPath =
            subFoldersByPath ?? new Dictionary<string, IReadOnlyList<string>>();

        public IEnumerable<string> GetSubFolders(string path)
            => _subFoldersByPath.TryGetValue(path, out var subFolders)
                ? subFolders
                : Enumerable.Empty<string>();

        public IEnumerable<string> GetSubFoldersSafe(string path) => GetSubFolders(path);

        public IEnumerable<string> GetDrives() => _drives;

        public bool DirectoryExists(string path) => _existingFolders.Contains(path);

        public string? GetParent(string path) => Path.GetDirectoryName(path);
    }
}
