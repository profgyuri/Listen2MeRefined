using Dapper;

namespace Listen2MeRefined.Infrastructure.Data;

public sealed record ParameterizedQuery(
    string QueryString,
    DynamicParameters Parameters);