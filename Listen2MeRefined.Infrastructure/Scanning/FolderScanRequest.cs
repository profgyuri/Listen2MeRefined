namespace Listen2MeRefined.Infrastructure.Scanning;

public readonly record struct FolderScanRequest(string Path, bool IncludeSubdirectories);
