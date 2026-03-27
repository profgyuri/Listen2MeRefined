namespace Listen2MeRefined.Application.Folders;

public sealed record FolderNavigationResult(bool Success, string FullPath, IReadOnlyList<string> Entries, string ErrorMessage = "");
