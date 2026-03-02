namespace Listen2MeRefined.Infrastructure.FolderBrowser;

/// <summary>
///     Provides read access to the system clipboard, abstracted for testability.
/// </summary>
public interface IClipboardService
{
    /// <summary>Returns the current clipboard text, or an empty string if none.</summary>
    string GetText();
}
