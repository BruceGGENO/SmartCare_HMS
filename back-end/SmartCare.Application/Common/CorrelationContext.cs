namespace SmartCare.Application.Common;

/// <summary>
/// Holds the current request correlation ID so audit rows can share it
/// without taking an HTTP dependency in Infrastructure.
/// </summary>
public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> Value = new();

    public static string? Current
    {
        get => Value.Value;
        set => Value.Value = value;
    }
}
