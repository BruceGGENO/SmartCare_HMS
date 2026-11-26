using SmartCare.Application.DTOs;

namespace SmartCare.Application.Interfaces.Services;

public interface IPatientEhrService
{
    Task<IReadOnlyList<PatientProfileDto>> SearchPatientsAsync(string? query, CancellationToken cancellationToken = default);
    Task<PatientProfileDto?> GetPatientByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientDuplicateCheckResult> CheckDuplicateAsync(string firstName, string lastName, DateTime dob, string phone, CancellationToken cancellationToken = default);
    Task<PatientProfileDto> RegisterPatientAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default);
    Task<AllergyDto> AddAllergyAsync(Guid patientId, AddAllergyRequest request, CancellationToken cancellationToken = default);
    Task<DiagnosisDto> AddDiagnosisAsync(Guid patientId, AddDiagnosisRequest request, string doctorName, CancellationToken cancellationToken = default);
    Task<TreatmentPlanDto> SaveTreatmentPlanAsync(Guid patientId, SaveTreatmentPlanRequest request, string doctorName, CancellationToken cancellationToken = default);
}
