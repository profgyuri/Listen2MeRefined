using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Core.Repositories;

/// <summary>
/// Specialized repository contract for audio metadata queries used by scan workflows.
/// </summary>
public interface IAudioRepository :
    IRepository<AudioModel>,
    IAdvancedDataReader<AdvancedFilter, AudioModel>,
    IFromFolderRemover
{
    /// <summary>
    /// Reads a single audio entry by exact file path (case-insensitive).
    /// </summary>
    /// <param name="path">Absolute path of the target audio file.</param>
    /// <returns>The matching audio entry, or <see langword="null"/> when none exists.</returns>
    Task<AudioModel?> ReadByPathAsync(string path);

    /// <summary>
    /// Reads audio entries scoped to a folder with optional recursive matching.
    /// </summary>
    /// <param name="folderPath">Base folder path used as scan scope.</param>
    /// <param name="includeSubdirectories">
    /// <see langword="true"/> to include descendant folders; otherwise only top-level files in the folder are returned.
    /// </param>
    /// <returns>The list of matching audio entries.</returns>
    Task<IReadOnlyList<AudioModel>> ReadByFolderScopeAsync(string folderPath, bool includeSubdirectories);

    /// <summary>
    /// Persists scanner changes in one batch transaction.
    /// </summary>
    /// <param name="toInsert">New audio entries to insert.</param>
    /// <param name="toUpdate">Existing audio entries to update.</param>
    /// <param name="toRemove">Existing audio entries to remove.</param>
    Task PersistScanChangesAsync(
        IReadOnlyCollection<AudioModel> toInsert,
        IReadOnlyCollection<AudioModel> toUpdate,
        IReadOnlyCollection<AudioModel> toRemove);
}
