using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Searching;

/// <summary>
/// Resolves how search results should be transferred into the default playlist.
/// </summary>
public interface ISearchResultsTransferService
{
    /// <summary>
    /// Resolves transfer actions for the current search-result state and user selection.
    /// </summary>
    /// <param name="allSearchResults">The complete set of currently visible search results.</param>
    /// <param name="selectedSearchResults">The currently selected search-result items.</param>
    /// <param name="transferMode">One of the enumeration values that specifies move or copy behavior.</param>
    /// <returns>A transfer decision describing songs to add, songs to remove, and selection cleanup behavior.</returns>
    SearchResultsTransferDecision ResolveTransfer(
        IEnumerable<AudioModel> allSearchResults,
        IEnumerable<AudioModel> selectedSearchResults,
        SearchResultsTransferMode transferMode);
}