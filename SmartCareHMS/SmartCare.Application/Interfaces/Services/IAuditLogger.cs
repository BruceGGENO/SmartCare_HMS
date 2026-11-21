namespace SmartCare.Application.Interfaces.Services
{
    public interface IAuditLogger
    {
        Task LogAsync(
            AuditEvent auditEvent,
            CancellationToken cancellationToken = default);
    }
}