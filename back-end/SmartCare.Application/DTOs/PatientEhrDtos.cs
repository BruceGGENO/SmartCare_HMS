namespace SmartCare.Application.DTOs;

public sealed class PatientProfileDto
{
    public Guid Id { get; set; }
    public string Mrn { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public List<AllergyDto> Allergies { get; set; } = [];
    public List<DiagnosisDto> Diagnoses { get; set; } = [];
    public List<TreatmentPlanDto> TreatmentPlans { get; set; } = [];
    public List<MedicalHistoryDto> MedicalHistory { get; set; } = [];
}

public sealed class RegisterPatientRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
}

public sealed class PatientDuplicateCheckResult
{
    public bool IsDuplicate { get; set; }
    public Guid? ExistingPatientId { get; set; }
    public string? ExistingMrn { get; set; }
    public string? Message { get; set; }
}

public sealed class AllergyDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string Substance { get; set; } = string.Empty;
    public string Severity { get; set; } = "Moderate"; // Mild, Moderate, Severe
    public string Reaction { get; set; } = string.Empty;
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class AddAllergyRequest
{
    public string Substance { get; set; } = string.Empty;
    public string Severity { get; set; } = "Moderate";
    public string Reaction { get; set; } = string.Empty;
}

public sealed class DiagnosisDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? EncounterId { get; set; }
    public string IcdCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiagnosedBy { get; set; } = string.Empty;
    public DateTimeOffset DiagnosedAtUtc { get; set; }
}

public sealed class AddDiagnosisRequest
{
    public string IcdCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? EncounterId { get; set; }
}

public sealed class TreatmentPlanDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Goals { get; set; } = string.Empty;
    public string Medications { get; set; } = string.Empty;
    public DateTime? FollowUpDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SaveTreatmentPlanRequest
{
    public string Goals { get; set; } = string.Empty;
    public string Medications { get; set; } = string.Empty;
    public DateTime? FollowUpDate { get; set; }
}

public sealed class MedicalHistoryDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string PastSurgeries { get; set; } = string.Empty;
    public string FamilyHistory { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset RecordedAtUtc { get; set; }
}
