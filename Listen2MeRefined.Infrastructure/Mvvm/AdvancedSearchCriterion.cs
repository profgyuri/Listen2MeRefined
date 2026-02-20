namespace Listen2MeRefined.Infrastructure.Mvvm;

public sealed record AdvancedSearchCriterion(
    string Field,
    string Relation,
    string RawValue,
    string NormalizedValue,
    AdvancedFilterOperator Operator)
{
    public string Display => $"{Field} {Relation} {RawValue}";
}
