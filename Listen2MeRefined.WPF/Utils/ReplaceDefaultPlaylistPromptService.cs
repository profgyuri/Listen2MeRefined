using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.ViewModels.Popups;

namespace Listen2MeRefined.WPF.Utils;

public sealed class ReplaceDefaultPlaylistPromptService : IReplaceDefaultPlaylistPrompt
{
    private readonly IWindowManager _windowManager;

    public ReplaceDefaultPlaylistPromptService(IWindowManager windowManager)
    {
        _windowManager = windowManager;
    }

    public async Task<bool> ConfirmReplaceAsync(int existingCount, int importedCount, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = await _windowManager.ShowPopupAsync<ReplaceDefaultPlaylistPopupViewModel>(
            WindowShowOptions.CenteredOnMainWindow(),
            vm => vm.SetCounts(existingCount, importedCount),
            ct);

        return result == true;
    }
}
