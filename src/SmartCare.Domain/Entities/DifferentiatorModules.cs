using SmartCare.Domain.Common;
namespace SmartCare.Domain.Entities;

// ---------------- MOD-07 Bed Reservation & Transfer ----------------
public class BedTransferRequest : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Guid EncounterId { get; set; }
    public string FromUnit { get; set; } = string.Empty;      // e.g. OT
    public string ToUnit { get; set; } = string.Empty;        // e.g. ICU
    public string? RequestedBedType { get; set; }
    public TransferPriority Priority { get; set; } = TransferPriority.Routine;
    public TransferStage Stage { get; set; } = TransferStage.Requested;
    [LongText] public string? ClinicalNotes { get; set; }
    public Guid? AssignedBedId { get; set; }
    public Bed? AssignedBed { get; set; }
    public Guid RequestedByStaffId { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? BedAssignedAtUtc { get; set; }
    public DateTime? DispatchedAtUtc { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? EscalatedAtUtc { get; set; }
}

// ---------------- MOD-08 Discharge Turnaround ----------------
public class Discharge : BaseEntity, IPhiEntity
{
    public Guid AdmissionId { get; set; }
    public Admission Admission { get; set; } = null!;
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public DischargeStage Stage { get; set; } = DischargeStage.Draft;
    [LongText] public string Summary { get; set; } = string.Empty;
    [LongText] public string? Medications { get; set; }
    [LongText] public string? FollowUpInstructions { get; set; }
    public DateTime? FinalizedAtUtc { get; set; }             // clinical content locked from here on
    public DateTime? CompletedAtUtc { get; set; }
    public ICollection<DischargeClearanceItem> Clearances { get; set; } = new List<DischargeClearanceItem>();
    public ICollection<DischargeAddendum> Addenda { get; set; } = new List<DischargeAddendum>();
}

public class DischargeClearanceItem : BaseEntity
{
    public Guid DischargeId { get; set; }
    public Discharge Discharge { get; set; } = null!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public ClearanceStatus Status { get; set; } = ClearanceStatus.Pending;
    public string? BlockReason { get; set; }
    public bool IsMandatory { get; set; } = true;
    public Guid? ClearedByUserId { get; set; }
    public DateTime? ClearedAtUtc { get; set; }
    public DateTime? LastReminderAtUtc { get; set; }
}

public class DischargeAddendum : BaseEntity, IPhiEntity
{
    public Guid DischargeId { get; set; }
    public Discharge Discharge { get; set; } = null!;
    public Guid PatientId { get; set; }
    [LongText] public string Text { get; set; } = string.Empty;
    public Guid AuthorStaffId { get; set; }
}

// ---------------- MOD-09 Waiting Room Analytics ----------------
public class PatientJourneyEvent : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid? AppointmentId { get; set; }
    public Guid? EncounterId { get; set; }
    public JourneyStation Station { get; set; }
    public JourneyEventType EventType { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? RecordedByUserId { get; set; }
    public bool IsManualCorrection { get; set; }
    public string? CorrectionReason { get; set; }
}

// ---------------- MOD-11 Predictive Occupancy ----------------
public class OccupancySnapshot : BaseEntity
{
    public Guid WardId { get; set; }
    public Ward Ward { get; set; } = null!;
    public DateOnly SnapshotDate { get; set; }
    public int OccupiedBeds { get; set; }
    public int TotalBeds { get; set; }
}

public class OccupancyForecast : BaseEntity
{
    public Guid WardId { get; set; }
    public Ward Ward { get; set; } = null!;
    public DateOnly ForecastDate { get; set; }
    public decimal PredictedOccupancy { get; set; }           // 0 to 1
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public ForecastMethod Method { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

// ---------------- MOD-12 Revenue vs Usage ----------------
/// <summary>Every consumed service (consultation, lab test, package session, procedure). Feeds the usage ledger and invoices.</summary>
public class ServiceEvent : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Guid? EncounterId { get; set; }
    public Guid? PriceItemId { get; set; }
    public Guid? PackagePurchaseId { get; set; }
    public Guid? DoctorId { get; set; }
    public string SourceModule { get; set; } = string.Empty;  // Consultation, Lab, Pharmacy, Package ...
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitValue { get; set; }            // price of ONE unit; total = Quantity x UnitValue
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Append-only. Corrections are new entries that reference the original.</summary>
public class RevenueUsageLedger : BaseEntity
{
    public LedgerEntryType EntryType { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? PackagePurchaseId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? ServiceEventId { get; set; }
    public Guid? DoctorId { get; set; }
    public decimal Amount { get; set; }                       // negative for reversals
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? ReferencesEntryId { get; set; }
    public RevenueUsageLedger? ReferencesEntry { get; set; }
    public string? Note { get; set; }
}

// ---------------- Audit ----------------
/// <summary>Append-only. Stores which fields changed, never their values, so the log itself never leaks PHI.</summary>
public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public Guid? PatientId { get; set; }
    public string? ChangedFields { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
