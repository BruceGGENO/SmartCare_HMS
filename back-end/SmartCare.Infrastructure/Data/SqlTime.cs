using System.Data.Common;

namespace SmartCare.Infrastructure.Data;

internal static class SqlTime
{
    public static DateTimeOffset Read(DbDataReader record, string column)
    {
        var value = record.GetValue(record.GetOrdinal(column));
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => DateTimeOffset.UtcNow
        };
    }

    public static DateTimeOffset? ReadOrNull(DbDataReader record, string column)
    {
        var ordinal = record.GetOrdinal(column);
        if (record.IsDBNull(ordinal))
            return null;
        return Read(record, column);
    }

    public static DateTime? ReadDateTimeOrNull(DbDataReader record, string column)
    {
        var ordinal = record.GetOrdinal(column);
        if (record.IsDBNull(ordinal))
            return null;
        var value = record.GetValue(ordinal);
        return value switch
        {
            DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            DateTimeOffset dto => dto.UtcDateTime,
            _ => Convert.ToDateTime(value)
        };
    }
}
