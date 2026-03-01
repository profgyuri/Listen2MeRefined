using System.Windows;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.WPF.Views;

namespace Listen2MeRefined.WPF.Utils;

public sealed class DroppedSongFolderPromptService : IDroppedSongFolderPromptService
{
    public Task<AddDroppedSongFolderDecision> PromptAsync(string folderPath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var window = new AddDroppedSongFolderWindow
        {
            Owner = Application.Current.MainWindow
        };

        window.SetFolderPath(folderPath);
        var result = window.ShowDialog();

        if (result == true)
        {
            return Task.FromResult(AddDroppedSongFolderDecision.AddFolder);
        }

        return Task.FromResult(window.DontAskAgainChecked
            ? AddDroppedSongFolderDecision.SkipAndDontAskAgain
            : AddDroppedSongFolderDecision.Skip);
    }
}
