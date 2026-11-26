using SmartCare.Application.DTOs.Responses;

namespace SmartCare.Application.Interfaces.Services;

public interface IPatientService
{
    Task<IReadOnlyList<PatientResponse>> SearchAsync(string? query, CancellationToken cancellationToken = default);

    Task<PatientResponse?> GetByIdAsync(int patientId, CancellationToken cancellationToken = default);

    Task<PatientResponse?> GetByMrnAsync(string mrn, CancellationToken cancellationToken = default);

    Task<PatientResponse> RegisterAsync(
        string mrn,
        string fullName,
        DateOnly? dateOfBirth,
        string? phone,
        string? sex,
        Guid? userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientAllergyResponse>> ListAllergiesAsync(int patientId, CancellationToken cancellationToken = default);

    Task<PatientAllergyResponse> AddAllergyAsync(
        int patientId,
        string substance,
        string? reaction,
        string? severity,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IcdCodeResponse>> SearchIcdAsync(string? query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientDiagnosisResponse>> ListDiagnosesAsync(int patientId, CancellationToken cancellationToken = default);

    Task<PatientDiagnosisResponse> AddDiagnosisAsync(
        int patientId,
        string icdCode,
        string? notes,
        Guid? diagnosedByUserId,
        CancellationToken cancellationToken = default);
}
