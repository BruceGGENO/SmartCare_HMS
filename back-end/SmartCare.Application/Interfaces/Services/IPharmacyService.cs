using SmartCare.Application.DTOs.Responses;

namespace SmartCare.Application.Interfaces.Services;

public interface IPharmacyService
{
    Task<IReadOnlyList<DrugResponse>> ListDrugsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrescriptionResponse>> ListPrescriptionsAsync(int? patientId, string? status, CancellationToken cancellationToken = default);
    Task<PrescriptionResponse?> GetPrescriptionAsync(int prescriptionId, CancellationToken cancellationToken = default);
    Task<PrescriptionResponse> PrescribeAsync(int patientId, int drugId, string dose, int quantity, Guid? prescribedByUserId, CancellationToken cancellationToken = default);
    Task<PrescriptionResponse> DispenseAsync(int prescriptionId, Guid? dispensedByUserId, CancellationToken cancellationToken = default);
}
