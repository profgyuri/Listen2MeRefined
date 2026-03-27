using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.ViewModels.Popups;
using Listen2MeRefined.Core.Enums;

namespace Listen2MeRefined.WPF.Utils;

public sealed class DroppedSongFolderPromptService : IDroppedSongFolderPromptService
{
    private readonly IWindowManager _windowManager;

    public DroppedSongFolderPromptService(IWindowManager windowManager)
    {
        _windowManager = windowManager;
    }

    public async Task<AddDroppedSongFolderDecision> PromptAsync(string folderPath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        SongDroppedPopupViewModel? popupViewModel = null;
        var result = await _windowManager.ShowPopupAsync<SongDroppedPopupViewModel>(
            WindowShowOptions.CenteredOnMainWindow(),
            vm =>
            {
                vm.SetFolderPath(folderPath);
                popupViewModel = vm;
            },
            ct);

        if (result == true)
        {
            return AddDroppedSongFolderDecision.AddFolder;
        }

        var dontAskAgain = popupViewModel?.DontAskAgain == true;
        return dontAskAgain
            ? AddDroppedSongFolderDecision.SkipAndDontAskAgain
            : AddDroppedSongFolderDecision.Skip;
    }
}
