namespace Listen2MeRefined.Infrastructure.Services.Models;

public readonly record struct FolderScanRequest(string Path, bool IncludeSubdirectories);
