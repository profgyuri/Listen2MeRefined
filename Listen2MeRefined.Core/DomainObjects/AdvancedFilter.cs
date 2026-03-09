using Listen2MeRefined.Core.Enums;

namespace Listen2MeRefined.Core.DomainObjects;

public sealed record AdvancedFilter(
    string Field,
    AdvancedFilterOperator Operator,
    string Value);