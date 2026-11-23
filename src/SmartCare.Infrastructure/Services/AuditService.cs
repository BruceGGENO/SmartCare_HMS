using SmartCare.Application.Common;
using SmartCare.Domain.Common;
using SmartCare.Domain.Entities;
using SmartCare.Infrastructure.Persistence;

namespace SmartCare.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public AuditService(AppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public Task LogReadAsync(Guid? patientId, string entityType, string? entityId, CancellationToken ct = default)
        => Write(AuditAction.Read, patientId, entityType, entityId, ct);

    public Task LogDeniedAsync(string entityType, string? entityId, CancellationToken ct = default)
        => Write(AuditAction.Denied, null, entityType, entityId, ct);

    private async Task Write(AuditAction action, Guid? patientId, string entityType, string? entityId, CancellationToken ct)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _user.UserId,
            UserName = _user.UserName,
            IpAddress = _user.IpAddress,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            PatientId = patientId
        });
        await _db.SaveChangesAsync(ct);
    }
}
