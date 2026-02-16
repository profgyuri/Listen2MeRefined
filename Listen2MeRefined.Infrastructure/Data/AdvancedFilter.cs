namespace Listen2MeRefined.Infrastructure.Data;

public sealed record AdvancedFilter(
    string Field,
    AdvancedFilterOperator Operator,
    string Value);