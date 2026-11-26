using System.Data.Common;
using SmartCare.Application.Common;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Auditing;

public sealed class AuditLogger : IAuditLogger
{
    private readonly ISqlExecutor _sql;

    public AuditLogger(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public async Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
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

        await _sql.ExecuteNonQueryAsync(
            "dbo.sp_WriteAuditLog",
            new Dictionary<string, object?>
            {
                ["Action"] = auditEvent.Action,
                ["EntityType"] = auditEvent.EntityType,
                ["EntityId"] = auditEvent.EntityId,
                ["ActorUserId"] = auditEvent.ActorUserId,
                ["Outcome"] = auditEvent.Outcome,
                ["OccurredAtUtc"] = auditEvent.OccurredAtUtc,
                ["CorrelationId"] = auditEvent.CorrelationId
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<AuditLogEntry>> ListRecentAsync(int take = 50, CancellationToken cancellationToken = default) =>
        _sql.QueryAsync(
            "dbo.sp_ListRecentAuditLogs",
            Map,
            new Dictionary<string, object?> { ["Take"] = take },
            cancellationToken);

    private static AuditLogEntry Map(DbDataReader record) => new()
    {
        AuditLogId = Convert.ToInt64(record.GetValue(record.GetOrdinal("AuditLogId"))),
        Action = record.Get<string>("Action"),
        EntityType = record.Get<string>("EntityType"),
        EntityId = record.GetStringOrNull("EntityId"),
        ActorUserId = record.GetStringOrNull("ActorUserId"),
        Outcome = record.Get<string>("Outcome"),
        OccurredAtUtc = record.GetValue(record.GetOrdinal("OccurredAtUtc")) is DateTimeOffset dto
            ? dto
            : DateTimeOffset.UtcNow,
        CorrelationId = record.GetStringOrNull("CorrelationId")
    };
}
