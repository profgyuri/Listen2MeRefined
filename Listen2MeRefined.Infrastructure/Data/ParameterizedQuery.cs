namespace Listen2MeRefined.Infrastructure.Data;
using global::Dapper;

public sealed record ParameterizedQuery(
    string QueryString,
    DynamicParameters Parameters);