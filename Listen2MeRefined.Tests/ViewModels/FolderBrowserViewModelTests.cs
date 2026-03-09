using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.FolderBrowser;
using Listen2MeRefined.Infrastructure.Settings;
using MediatR;
using Moq;
using Serilog;
using FolderBrowserViewModel = Listen2MeRefined.Infrastructure.ViewModels.FolderBrowserViewModel;

namespace Listen2MeRefined.Tests.ViewModels;

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

    [Fact]
    public async Task NavigateToClipboardPathCommand_ValidFolderInClipboard_NavigatesSuccessfully()
    {
        var settings = new AppSettings();
        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"]),
            clipboardText: @"C:\Music",
            out _);

        await viewModel.InitializeAsync();

        viewModel.NavigateToClipboardPathCommand.Execute(null);

        Assert.Equal(@"C:\Music", viewModel.FullPath);
        Assert.False(viewModel.HasValidationError);
    }

    [Fact]
    public async Task NavigateToClipboardPathCommand_InvalidPathInClipboard_SetsValidationError()
    {
        var settings = new AppSettings();
        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"]),
            clipboardText: @"Z:\NotHere",
            out _);

        await viewModel.InitializeAsync();

        viewModel.NavigateToClipboardPathCommand.Execute(null);

        Assert.True(viewModel.HasValidationError);
        Assert.Contains("Could not open", viewModel.ValidationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NavigateToClipboardPathCommand_EmptyClipboard_SetsValidationError()
    {
        var settings = new AppSettings();
        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"]),
            clipboardText: "",
            out _);

        await viewModel.InitializeAsync();

        viewModel.NavigateToClipboardPathCommand.Execute(null);

        Assert.True(viewModel.HasValidationError);
        Assert.Contains("valid path", viewModel.ValidationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NavigateToClipboardPathCommand_QuotedFolderPath_StripsQuotesAndNavigates()
    {
        // Windows 'Copy as Path' wraps paths in double-quotes, e.g. "C:\Music"
        var settings = new AppSettings();
        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"]),
            clipboardText: "\"C:\\Music\"",
            out _);

        await viewModel.InitializeAsync();

        viewModel.NavigateToClipboardPathCommand.Execute(null);

        Assert.Equal(@"C:\Music", viewModel.FullPath);
        Assert.False(viewModel.HasValidationError);
    }

    [Fact]
    public async Task NavigateToClipboardPathCommand_TrailingBackslash_StripsBackslashAndNavigates()
    {
        // A path ending with '\' confuses Path.GetDirectoryName – the trailing separator must be removed.
        var settings = new AppSettings();
        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"]),
            clipboardText: @"C:\Music\",
            out _);

        await viewModel.InitializeAsync();

        viewModel.NavigateToClipboardPathCommand.Execute(null);

        Assert.Equal(@"C:\Music", viewModel.FullPath);
        Assert.False(viewModel.HasValidationError);
    }

    [Fact]
    public async Task NavigateToClipboardPathCommand_QuotedPathWithTrailingBackslash_StripsQuotesAndBackslashAndNavigates()
    {
        // Windows 'Copy as Path' on some folders produces "C:\Music\" – both issues must be handled.
        var settings = new AppSettings();
        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"]),
            clipboardText: "\"C:\\Music\\\"",
            out _);

        await viewModel.InitializeAsync();

        viewModel.NavigateToClipboardPathCommand.Execute(null);

        Assert.Equal(@"C:\Music", viewModel.FullPath);
        Assert.False(viewModel.HasValidationError);
    }

    private static FolderBrowserViewModel CreateViewModel(
        AppSettings settings,
        IFolderBrowser folderBrowser,
        out Mock<IMediator> mediator) =>
        CreateViewModel(settings, folderBrowser, clipboardText: string.Empty, out mediator);

    private static FolderBrowserViewModel CreateViewModel(
        AppSettings settings,
        IFolderBrowser folderBrowser,
        string clipboardText,
        out Mock<IMediator> mediator)
    {
        var clipboardService = new FakeClipboardService(clipboardText);

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
            settingsWriteService,
            clipboardService);
    }

    private sealed class FakeClipboardService(string text) : IClipboardService
    {
        public string GetText() => text;
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
