using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Searching;

public sealed class SearchResultsTransferService : ISearchResultsTransferService
{
    /// <inheritdoc />
    public SearchResultsTransferDecision ResolveTransfer(
        IEnumerable<AudioModel> allSearchResults,
        IEnumerable<AudioModel> selectedSearchResults,
        SearchResultsTransferMode transferMode)
    {
        var selected = selectedSearchResults.ToArray();
        var songsToAdd = selected.Length > 0
            ? selected
            : allSearchResults.ToArray();

        var songsToRemove = transferMode == SearchResultsTransferMode.Move
            ? songsToAdd
            : Array.Empty<AudioModel>();

        return new SearchResultsTransferDecision(
            songsToAdd,
            songsToRemove,
            ClearSelection: true);
    }
}
