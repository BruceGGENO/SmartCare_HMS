namespace SmartCare.Application.Interfaces.Services
{
    public class AuditEvent
    {
        public string Action { get; set; } = string.Empty;

        public string EntityType { get; set; } = string.Empty;

        public string? EntityId { get; set; }

        public string? ActorUserId { get; set; }

        public string Outcome { get; set; } = string.Empty;

        public DateTimeOffset OccurredAtUtc { get; set; }

        public string? CorrelationId { get; set; }
    }
}