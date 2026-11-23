namespace SmartCare.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>Marks entities that hold protected health information. Every change is audit-logged.</summary>
public interface IPhiEntity { }

/// <summary>Entities that need optimistic concurrency (beds, appointment slots).</summary>
public interface IConcurrent { byte[] RowVersion { get; set; } }

/// <summary>Long free-text column (nvarchar(max)). Other strings default to 256 characters.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class LongTextAttribute : Attribute { }
