using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.ViewModels;

public sealed record AdvancedSearchCriterion(
    string Field,
    string Relation,
    string RawValue,
    string NormalizedValue,
    AdvancedFilterOperator Operator)
{
    public string Display => $"{Field} {Relation} {RawValue}";
}
