using Listen2MeRefined.Core.Enums;

namespace Listen2MeRefined.Application.Searching;

public sealed record AdvancedSearchCriterion(
    string Field,
    string Relation,
    string RawValue,
    string NormalizedValue,
    AdvancedFilterOperator Operator)
{
    public string Display => $"{Field} {Relation} {RawValue}";
}
