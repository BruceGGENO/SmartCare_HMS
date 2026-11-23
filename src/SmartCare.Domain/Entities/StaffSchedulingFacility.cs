using SmartCare.Domain.Common;
namespace SmartCare.Domain.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDischargeClearanceDepartment { get; set; }
    public bool IsMandatoryClearance { get; set; }
}

public class Staff : BaseEntity
{
    public Guid? UserId { get; set; }                         // links to the Identity user, if the staff member can log in
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string? Specialty { get; set; }
    public string? LicenseNumber { get; set; }
    public DateOnly? LicenseExpiry { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Shift : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ShiftType Type { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

public class DutyRoster : BaseEntity
{
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public bool IsPublished { get; set; }
    public ICollection<RosterAssignment> Assignments { get; set; } = new List<RosterAssignment>();
}

public class RosterAssignment : BaseEntity
{
    public Guid RosterId { get; set; }
    public DutyRoster Roster { get; set; } = null!;
    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
    public Guid ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public DateOnly Date { get; set; }
}

public class LeaveRequest : BaseEntity
{
    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string? Reason { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public Guid? DecidedByUserId { get; set; }
}

public class Attendance : BaseEntity
{
    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
    public DateTime CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }
}

public class DoctorAvailability : BaseEntity
{
    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotMinutes { get; set; } = 20;
}

/// <summary>Breaks, leave or blocked periods. These slots are never offered for booking.</summary>
public class DoctorTimeOff : BaseEntity
{
    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? Reason { get; set; }
}

public class Appointment : BaseEntity, IPhiEntity, IConcurrent
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid DoctorId { get; set; }
    public Staff Doctor { get; set; } = null!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
    public int? QueueNumber { get; set; }                     // walk-ins
    public string? CancellationReason { get; set; }
    public bool IsLateCancellation { get; set; }
    public string? QrTokenHash { get; set; }                  // only the hash is stored; token is signed and single-use
    public DateTime? QrExpiresAtUtc { get; set; }
    public DateTime? QrUsedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<AppointmentStatusHistory> History { get; set; } = new List<AppointmentStatusHistory>();
}

public class AppointmentStatusHistory : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
    public AppointmentStatus FromStatus { get; set; }
    public AppointmentStatus ToStatus { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}

public class MessageLog : BaseEntity
{
    public MessageChannel Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    [LongText] public string Body { get; set; } = string.Empty;
    public MessageStatus Status { get; set; } = MessageStatus.Pending;
    public int Attempts { get; set; }
    public string? Error { get; set; }
    public Guid? AppointmentId { get; set; }
    public DateTime? SentAtUtc { get; set; }
}

public class Ward : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string UnitType { get; set; } = "General";         // General, ICU, OT, Maternity ...
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}

public class Room : BaseEntity
{
    public Guid WardId { get; set; }
    public Ward Ward { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public ICollection<Bed> Beds { get; set; } = new List<Bed>();
}

public class Bed : BaseEntity, IConcurrent
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string BedType { get; set; } = "Standard";
    public BedStatus Status { get; set; } = BedStatus.Available;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
