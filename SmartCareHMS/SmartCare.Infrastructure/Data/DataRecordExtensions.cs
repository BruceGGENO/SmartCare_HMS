// Location: SmartCare.Infrastructure/Data/DataRecordExtensions.cs
// Small helpers so mapper lambdas stay short and NULL-safe:
//   r => new PatientDto(r.Get<int>("PatientId"), r.Get<string>("Mrn"), r.GetStringOrNull("Phone"))
using System.Data;

namespace SmartCare.Infrastructure.Data;

public static class DataRecordExtensions
{
    /// <summary>Reads a NOT NULL column. Throws a clear error if the value is NULL.</summary>
    public static T Get<T>(this IDataRecord record, string column)
    {
        var ordinal = record.GetOrdinal(column);
        if (record.IsDBNull(ordinal))
            throw new InvalidOperationException($"Column '{column}' is NULL. Use GetOrNull<T> or GetStringOrNull.");
        return Convert<T>(record.GetValue(ordinal));
    }

    /// <summary>Reads a nullable value-type column (int?, decimal?, DateTime?, Guid? ...).</summary>
    public static T? GetOrNull<T>(this IDataRecord record, string column) where T : struct
    {
        var ordinal = record.GetOrdinal(column);
        return record.IsDBNull(ordinal) ? null : Convert<T>(record.GetValue(ordinal));
    }

    /// <summary>Reads a nullable string column.</summary>
    public static string? GetStringOrNull(this IDataRecord record, string column)
    {
        var ordinal = record.GetOrdinal(column);
        return record.IsDBNull(ordinal) ? null : record.GetString(ordinal);
    }

    // Handles SQL type mismatches such as smallint -> int or decimal -> double.
    private static T Convert<T>(object value)
    {
        if (value is T direct) return direct;
        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)System.Convert.ChangeType(value, target);
    }
}
