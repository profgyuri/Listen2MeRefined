using Listen2MeRefined.Core.Enums;

namespace Listen2MeRefined.Infrastructure.Settings;

public interface IDroppedSongFolderPromptService
{
    Task<AddDroppedSongFolderDecision> PromptAsync(string folderPath, CancellationToken ct = default);
}
