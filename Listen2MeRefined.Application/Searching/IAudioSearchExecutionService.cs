using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Searching;

/// <summary>
/// Executes quick and advanced audio searches against configured data sources.
/// </summary>
public interface IAudioSearchExecutionService
{
    /// <summary>Executes quick search for a search term.</summary>
    Task<IReadOnlyList<AudioModel>> ExecuteQuickSearchAsync(string searchTerm);
    /// <summary>Executes advanced search using filters and match mode.</summary>
    Task<IReadOnlyList<AudioModel>> ExecuteAdvancedSearchAsync(IEnumerable<AdvancedFilter> filters, SearchMatchMode matchMode);
}
