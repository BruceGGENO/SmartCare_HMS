namespace SmartCare.Application.Common
{
    /// <summary>
    /// Holds the current request's correlation ID so audit rows and ILogger
    /// scopes can share the same value without taking an HTTP dependency.
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
}
