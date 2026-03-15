using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Searching;

/// <summary>
/// Represents the calculated actions for a search-result transfer operation.
/// </summary>
/// <param name="SongsToAdd">The songs that should be added to the default playlist.</param>
/// <param name="SongsToRemove">The songs that should be removed from the source results list.</param>
/// <param name="ClearSelection"><see langword="true" /> to clear search-result selection after transfer; otherwise, <see langword="false" />.</param>
public sealed record SearchResultsTransferDecision(
    IReadOnlyList<AudioModel> SongsToAdd,
    IReadOnlyList<AudioModel> SongsToRemove,
    bool ClearSelection);