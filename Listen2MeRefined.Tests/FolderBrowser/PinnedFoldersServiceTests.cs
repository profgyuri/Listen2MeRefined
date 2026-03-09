using Listen2MeRefined.Application.Folders;
using Listen2MeRefined.Infrastructure.FolderBrowser;

namespace Listen2MeRefined.Tests.FolderBrowser;

public sealed class PinnedFoldersServiceTests
{
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
