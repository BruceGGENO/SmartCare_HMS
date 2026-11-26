namespace SmartCare.Application.DTOs.Responses;

public sealed class AuditLogResponse
{
    public long AuditLogId { get; init; }

    public string Action { get; init; } = string.Empty;

    public string EntityType { get; init; } = string.Empty;

    public string? EntityId { get; init; }

    public string? ActorUserId { get; init; }

    public string Outcome { get; init; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; init; }

    public string? CorrelationId { get; init; }
}
