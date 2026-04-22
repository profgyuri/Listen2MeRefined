using Listen2MeRefined.Application.Playlist;
using Microsoft.Win32;

namespace Listen2MeRefined.WPF.Utils;

public sealed class FileDialogService : IFileDialogService
{
    public string? PickOpenFile(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickSaveFile(string title, string filter, string defaultFileName, string defaultExtension)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = defaultFileName,
            DefaultExt = defaultExtension,
            OverwritePrompt = true,
            AddExtension = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
