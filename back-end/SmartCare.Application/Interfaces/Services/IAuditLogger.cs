namespace SmartCare.Application.Interfaces.Services;

public interface IAuditLogger
{
    Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogEntry>> ListRecentAsync(int take = 50, CancellationToken cancellationToken = default);
}
