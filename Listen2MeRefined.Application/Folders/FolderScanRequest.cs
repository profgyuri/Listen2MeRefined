namespace Listen2MeRefined.Application.Folders;

public readonly record struct FolderScanRequest(string Path, bool IncludeSubdirectories);
