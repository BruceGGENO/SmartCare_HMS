namespace SmartCare.Application.Interfaces.Security
{
    public sealed class OutboxMessage
    {
        public long OutboxId { get; set; }

        public string Channel { get; set; } = string.Empty;

        public string Recipient { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
