namespace Listen2MeRefined.Infrastructure.Services.Models;

public sealed record FolderNavigationResult(bool Success, string FullPath, IReadOnlyList<string> Entries, string ErrorMessage = "");
