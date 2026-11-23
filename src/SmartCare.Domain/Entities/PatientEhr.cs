using SmartCare.Domain.Common;
namespace SmartCare.Domain.Entities;

public class Patient : BaseEntity, IPhiEntity
{
    public string Mrn { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? NationalId { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Allergy> Allergies { get; set; } = new List<Allergy>();
    public ICollection<InsuranceCoverage> Insurances { get; set; } = new List<InsuranceCoverage>();
    public ICollection<Encounter> Encounters { get; set; } = new List<Encounter>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}

public class InsuranceCoverage : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public string Scheme { get; set; } = string.Empty;       // e.g. RSSB/RAMA, Mutuelle de Sante, private insurer
    public string MemberNumber { get; set; } = string.Empty;
    public decimal CoveragePercent { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

public class Allergy : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public string Substance { get; set; } = string.Empty;
    public AllergySeverity Severity { get; set; }
    public string? Reaction { get; set; }
}

public class MedicalHistoryEntry : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public HistoryKind Kind { get; set; }
    [LongText] public string Description { get; set; } = string.Empty;
    public Guid AuthorStaffId { get; set; }
    public Staff AuthorStaff { get; set; } = null!;
}

public class Encounter : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid DoctorId { get; set; }
    public Staff Doctor { get; set; } = null!;
    public EncounterType Type { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public string? ChiefComplaint { get; set; }
    public ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();
    public ICollection<ClinicalNote> Notes { get; set; } = new List<ClinicalNote>();
    public ICollection<Vitals> Vitals { get; set; } = new List<Vitals>();
}

public class Admission : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid EncounterId { get; set; }
    public Encounter Encounter { get; set; } = null!;
    public Guid? CurrentBedId { get; set; }
    public Bed? CurrentBed { get; set; }
    public AdmissionStatus Status { get; set; } = AdmissionStatus.Admitted;
    public DateTime AdmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DischargedAtUtc { get; set; }
}

public class Vitals : BaseEntity, IPhiEntity
{
    public Guid EncounterId { get; set; }
    public Encounter Encounter { get; set; } = null!;
    public Guid PatientId { get; set; }
    public Guid RecordedByStaffId { get; set; }
    public decimal? TemperatureC { get; set; }
    public int? HeartRate { get; set; }
    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public int? RespiratoryRate { get; set; }
    public decimal? OxygenSaturation { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public int? TriageLevel { get; set; }                    // 1 (most urgent) to 5
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Diagnosis : BaseEntity, IPhiEntity
{
    public Guid EncounterId { get; set; }
    public Encounter Encounter { get; set; } = null!;
    public string IcdCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public DateTime DiagnosedAtUtc { get; set; } = DateTime.UtcNow;
}

public class TreatmentPlan : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid? EncounterId { get; set; }
    public int Version { get; set; } = 1;
    public bool IsCurrent { get; set; } = true;
    [LongText] public string Goals { get; set; } = string.Empty;
    [LongText] public string? Medications { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public Guid AuthorStaffId { get; set; }
}

public class ClinicalNote : BaseEntity, IPhiEntity
{
    public Guid EncounterId { get; set; }
    public Encounter Encounter { get; set; } = null!;
    public Guid PatientId { get; set; }
    public Guid AuthorStaffId { get; set; }
    [LongText] public string Text { get; set; } = string.Empty;
    public bool IsShareableWithPatient { get; set; }
}

public class PatientDocument : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid? EncounterId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
}

public class Consent : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public string ConsentType { get; set; } = string.Empty;  // DataSharing, Sms, Email
    public bool Granted { get; set; }
    public int Version { get; set; } = 1;
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
