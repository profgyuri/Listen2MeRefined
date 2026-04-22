namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Abstracts the WPF open/save file dialogs so ViewModels stay framework-agnostic.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Shows an open-file dialog using the supplied <paramref name="filter"/> string and returns the selected path, or <see langword="null"/> when cancelled.
    /// </summary>
    string? PickOpenFile(string title, string filter);

    /// <summary>
    /// Shows a save-file dialog using the supplied <paramref name="filter"/> string and returns the selected path, or <see langword="null"/> when cancelled.
    /// </summary>
    string? PickSaveFile(string title, string filter, string defaultFileName, string defaultExtension);
}
