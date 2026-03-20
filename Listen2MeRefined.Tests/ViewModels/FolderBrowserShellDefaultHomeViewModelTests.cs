using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Folders;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Infrastructure.FolderBrowser;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class FolderBrowserShellDefaultHomeViewModelTests
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
    public async Task TryHandleSelectedPathAsync_ValidPath_PublishesMessage()
    {
        var settings = new AppSettings();
        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"]),
            out var messenger);

        await viewModel.InitializeAsync();
        viewModel.FullPath = @"C:\Music";

        string? selectedPath = null;
        var recipient = new object();
        messenger.Register<object, FolderBrowserPathSelectedMessage>(recipient, (_, message) => selectedPath = message.Value);

        var result = await viewModel.TryHandleSelectedPathAsync();

        Assert.True(result);
        Assert.Equal(@"C:\Music", selectedPath);
        messenger.UnregisterAll(recipient);
    }

    [Fact]
    public async Task TogglePinCommand_PublishesPinnedFoldersChangedMessage()
    {
        var settings = new AppSettings();
        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"]),
            out var messenger);

        await viewModel.InitializeAsync();
        viewModel.FullPath = @"C:\Music";

        IReadOnlyCollection<string>? publishedPins = null;
        var recipient = new object();
        messenger.Register<object, PinnedFoldersChangedMessage>(recipient, (_, message) => publishedPins = message.Value);

        await viewModel.TogglePinCommand.ExecuteAsync(null);

        Assert.NotNull(publishedPins);
        Assert.Contains(@"C:\Music", publishedPins!);
        messenger.UnregisterAll(recipient);
    }

    [Fact]
    public async Task NavigateToClipboardPathCommand_QuotedFolderPath_StripsQuotesAndNavigates()
    {
        var settings = new AppSettings();
        var viewModel = CreateViewModel(
            settings,
            new FakeFolderBrowser(
                drives: [@"C:\"],
                existingFolders: [@"C:\Music"]),
            clipboardText: "\"C:\\Music\"",
            out _);

        await viewModel.InitializeAsync();

        await viewModel.NavigateToClipboardPathCommand.ExecuteAsync(null);

        Assert.Equal(@"C:\Music", viewModel.FullPath);
        Assert.False(viewModel.HasValidationError);
    }

    private static FolderBrowserShellDefaultHomeViewModel CreateViewModel(
        AppSettings settings,
        IFolderBrowser folderBrowser,
        out WeakReferenceMessenger messenger) =>
        CreateViewModel(settings, folderBrowser, clipboardText: string.Empty, out messenger);

    private static FolderBrowserShellDefaultHomeViewModel CreateViewModel(
        AppSettings settings,
        IFolderBrowser folderBrowser,
        string clipboardText,
        out WeakReferenceMessenger messenger)
    {
        var clipboardService = new FakeClipboardService(clipboardText);

        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);
        settingsManager
            .Setup(x => x.SaveSettings(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(apply => apply(settings));

        var settingsReadService = new AppSettingsReader(settingsManager.Object);
        var settingsWriteService = new AppSettingsWriter(settingsManager.Object);
        var folderNavigationService = new FolderNavigationService(folderBrowser);
        var pinnedFoldersService = new PinnedFoldersService(folderBrowser);
        messenger = new WeakReferenceMessenger();
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        return new FolderBrowserShellDefaultHomeViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
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
