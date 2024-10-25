namespace Listen2MeRefined.WPF;
using System;
using System.Data;
using Dapper;

public sealed class TimeSpanTypeHandler : SqlMapper.TypeHandler<TimeSpan>
{
    /// <inheritdoc />
    public override void SetValue(
        IDbDataParameter parameter,
        TimeSpan value)
    {
        parameter.Value = value.ToString();
    }

    public override TimeSpan Parse(object value)
    {
        return TimeSpan.Parse((string) value);
    }
}