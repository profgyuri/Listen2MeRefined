namespace Listen2MeRefined.Infrastructure.FolderBrowser;

public sealed record FolderNavigationResult(bool Success, string FullPath, IReadOnlyList<string> Entries, string ErrorMessage = "");
