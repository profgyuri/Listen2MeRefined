using Listen2MeRefined.Infrastructure.FolderBrowser;
using Listen2MeRefined.Infrastructure.Services;

namespace Listen2MeRefined.Tests.Services;

public sealed class FolderAndPinnedServicesTests
{
    [Fact]
    public void ResolveInitialPath_PrefersLastBrowsedWhenEnabledAndValid()
    {
        var folderBrowser = new FakeFolderBrowser(
            drives: [@"C:\"],
            existingFolders: [@"C:\Music"],
            subFoldersByPath: new Dictionary<string, IReadOnlyList<string>>());
        var sut = new FolderNavigationService(folderBrowser);

        var result = sut.ResolveInitialPath(true, @"C:\Music", [@"D:\Other"]);

        Assert.Equal(@"C:\Music", result);
    }

    [Fact]
    public void NavigateToPath_InvalidPath_ReturnsFailure()
    {
        var folderBrowser = new FakeFolderBrowser(
            drives: [@"C:\"],
            existingFolders: [@"C:\Music"],
            subFoldersByPath: new Dictionary<string, IReadOnlyList<string>>());
        var sut = new FolderNavigationService(folderBrowser);

        var result = sut.NavigateToPath(@"Z:\Missing");

        Assert.False(result.Success);
        Assert.Contains("Could not open", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NavigateToPath_ValidPath_IncludesParentItem()
    {
        var folderBrowser = new FakeFolderBrowser(
            drives: [@"C:\"],
            existingFolders: [@"C:\Music"],
            subFoldersByPath: new Dictionary<string, IReadOnlyList<string>>
            {
                [@"C:\Music"] = ["Rock", "Jazz"]
            });
        var sut = new FolderNavigationService(folderBrowser);

        var result = sut.NavigateToPath(@"C:\Music");

        Assert.True(result.Success);
        Assert.Equal(@"C:\Music", result.FullPath);
        Assert.Contains("..", result.Entries);
        Assert.Contains("Rock", result.Entries);
        Assert.Contains("Jazz", result.Entries);
    }

    [Fact]
    public void ApplyFilter_CaseInsensitive_FiltersEntries()
    {
        var sut = new FolderNavigationService(new FakeFolderBrowser([@"C:\"], [], new Dictionary<string, IReadOnlyList<string>>()));

        var result = sut.ApplyFilter(["Rock", "Jazz", "HipHop"], "ja");

        var single = Assert.Single(result);
        Assert.Equal("Jazz", single);
    }

    [Fact]
    public void NormalizeExisting_RemovesMissingAndDuplicates()
    {
        var folderBrowser = new FakeFolderBrowser(
            drives: [@"C:\"],
            existingFolders: [@"C:\A", @"D:\B"],
            subFoldersByPath: new Dictionary<string, IReadOnlyList<string>>());
        var sut = new PinnedFoldersService(folderBrowser);

        var result = sut.NormalizeExisting([@"C:\A", @"c:\a", @"Z:\Missing", @"D:\B"]);

        Assert.Equal(2, result.Count);
        Assert.Contains(@"C:\A", result, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(@"D:\B", result, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void TogglePinnedFolder_AddsAndRemovesPath()
    {
        var folderBrowser = new FakeFolderBrowser(
            drives: [@"C:\"],
            existingFolders: [@"C:\A"],
            subFoldersByPath: new Dictionary<string, IReadOnlyList<string>>());
        var sut = new PinnedFoldersService(folderBrowser);

        var added = sut.TogglePinnedFolder([], @"C:\A");
        Assert.Single(added);

        var removed = sut.TogglePinnedFolder(added, @"C:\A");
        Assert.Empty(removed);
    }

    private sealed class FakeFolderBrowser(
        IReadOnlyList<string> drives,
        IReadOnlyCollection<string> existingFolders,
        IReadOnlyDictionary<string, IReadOnlyList<string>> subFoldersByPath) : IFolderBrowser
    {
        private readonly IReadOnlyList<string> _drives = drives;
        private readonly IReadOnlyCollection<string> _existingFolders = existingFolders;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _subFoldersByPath = subFoldersByPath;

        public IEnumerable<string> GetSubFolders(string path)
        {
            return _subFoldersByPath.TryGetValue(path, out var subFolders)
                ? subFolders
                : [];
        }

        public IEnumerable<string> GetSubFoldersSafe(string path) => GetSubFolders(path);

        public IEnumerable<string> GetDrives() => _drives;

        public bool DirectoryExists(string path) => _existingFolders.Contains(path, StringComparer.OrdinalIgnoreCase);

        public string? GetParent(string path) => Path.GetDirectoryName(path);
    }
}
