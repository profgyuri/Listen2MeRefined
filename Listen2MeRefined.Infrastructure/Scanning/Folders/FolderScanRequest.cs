namespace Listen2MeRefined.Infrastructure.Scanning.Folders;

public readonly record struct FolderScanRequest(string Path, bool IncludeSubdirectories);
