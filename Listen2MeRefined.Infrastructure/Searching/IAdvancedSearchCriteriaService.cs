using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.ViewModels;

namespace Listen2MeRefined.Infrastructure.Searching;

/// <summary>
/// Provides metadata and validation logic for advanced-search criteria.
/// </summary>
public interface IAdvancedSearchCriteriaService
{
    /// <summary>Gets searchable column names for advanced search.</summary>
    IReadOnlyList<string> GetColumnNames();
    /// <summary>Gets relation options and suffix text for a selected column.</summary>
    SearchRelationDefinition GetRelationDefinition(string columnName);
    /// <summary>Builds and validates a criterion from raw UI selections and input.</summary>
    AdvancedCriteriaBuildResult BuildCriterion(string selectedColumnName, string selectedRelation, string inputText);
    /// <summary>Determines whether the current raw values can form a valid criterion.</summary>
    bool CanBuildCriterion(string selectedColumnName, string selectedRelation, string inputText);
    /// <summary>Converts criteria records into executable advanced filters.</summary>
    IReadOnlyList<AdvancedFilter> BuildFilters(IEnumerable<AdvancedSearchCriterion> criterias);
}
