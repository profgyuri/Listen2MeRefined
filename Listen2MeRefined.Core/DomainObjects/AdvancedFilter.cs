using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Core.DomainObjects;

public sealed record AdvancedFilter(
    string Field,
    AdvancedFilterOperator Operator,
    string Value);