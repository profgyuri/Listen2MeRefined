using Dapper;

namespace Listen2MeRefined.Infrastructure.Data;

internal sealed record ParameterizedQuery(
    string QueryString,
    DynamicParameters Parameters);