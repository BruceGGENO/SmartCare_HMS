using Microsoft.EntityFrameworkCore;

namespace SmartCare.Application.Common;

/// <summary>What the Application layer may do with the database. Implemented in Infrastructure.</summary>
public interface IAppDbContext
{
    DbSet<T> Set<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? StaffId { get; }
    string? UserName { get; }
    string? IpAddress { get; }
    bool IsInRole(string role);
}

public interface IAuditService
{
    /// <summary>Records that a user viewed PHI. Writes (create/update/delete) are logged automatically on SaveChanges.</summary>
    Task LogReadAsync(Guid? patientId, string entityType, string? entityId, CancellationToken ct = default);
    Task LogDeniedAsync(string entityType, string? entityId, CancellationToken ct = default);
}
