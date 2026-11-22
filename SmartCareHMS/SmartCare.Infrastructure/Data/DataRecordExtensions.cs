using System.Data;

namespace SmartCare.Infrastructure.Data;

public static class DataRecordExtensions
{
    public static T Get<T>(this IDataRecord record, string column)
    {
        var ordinal = record.GetOrdinal(column);
        if (record.IsDBNull(ordinal))
            throw new InvalidOperationException($"Column '{column}' is NULL. Use GetOrNull<T> or GetStringOrNull.");
        return Convert<T>(record.GetValue(ordinal));
    }

    public static T? GetOrNull<T>(this IDataRecord record, string column) where T : struct
    {
        var ordinal = record.GetOrdinal(column);
        return record.IsDBNull(ordinal) ? null : Convert<T>(record.GetValue(ordinal));
    }

    public static string? GetStringOrNull(this IDataRecord record, string column)
    {
        var ordinal = record.GetOrdinal(column);
        return record.IsDBNull(ordinal) ? null : record.GetString(ordinal);
    }

    private static T Convert<T>(object value)
    {
        if (value is T direct) return direct;
        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)System.Convert.ChangeType(value, target);
    }
}
