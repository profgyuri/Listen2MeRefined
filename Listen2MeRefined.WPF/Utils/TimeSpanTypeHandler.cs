using System;
using System.Data;
using Dapper;

namespace Listen2MeRefined.WPF;

public sealed class TimeSpanTypeHandler : SqlMapper.TypeHandler<TimeSpan>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, TimeSpan value)
    {
        parameter.Value = value.ToString();
    }

    public override TimeSpan Parse(object value)
        => TimeSpan.Parse((string)value);
}