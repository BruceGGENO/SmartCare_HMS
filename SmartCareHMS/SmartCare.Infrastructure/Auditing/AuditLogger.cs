using SmartCare.Application.Common;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;

namespace SmartCare.Infrastructure.Auditing;

public sealed class AuditLogger : IAuditLogger
{
    public const string WriteProcedure = "dbo.sp_WriteAuditLog";

    private readonly ISqlExecutor _sql;

    public AuditLogger(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public async Task LogAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        if (string.IsNullOrWhiteSpace(auditEvent.Action))
            throw new ArgumentException("Action is required.", nameof(auditEvent));
        if (string.IsNullOrWhiteSpace(auditEvent.EntityType))
            throw new ArgumentException("EntityType is required.", nameof(auditEvent));
        if (string.IsNullOrWhiteSpace(auditEvent.Outcome))
            throw new ArgumentException("Outcome is required.", nameof(auditEvent));

        if (auditEvent.OccurredAtUtc == default)
            auditEvent.OccurredAtUtc = DateTimeOffset.UtcNow;

        auditEvent.CorrelationId ??= CorrelationContext.Current;

        var parameters = new Dictionary<string, object?>
        {
            ["Action"] = auditEvent.Action,
            ["EntityType"] = auditEvent.EntityType,
            ["EntityId"] = auditEvent.EntityId,
            ["ActorUserId"] = auditEvent.ActorUserId,
            ["Outcome"] = auditEvent.Outcome,
            ["OccurredAtUtc"] = auditEvent.OccurredAtUtc,
            ["CorrelationId"] = auditEvent.CorrelationId
        };

        await _sql.ExecuteNonQueryAsync(WriteProcedure, parameters, cancellationToken);
    }
}
