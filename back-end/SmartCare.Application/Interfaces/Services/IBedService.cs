using SmartCare.Application.DTOs.Responses;

namespace SmartCare.Application.Interfaces.Services;

public interface IBedService
{
    Task<IReadOnlyList<WardResponse>> ListWardsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BedResponse>> ListBedsAsync(int? wardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdmissionResponse>> ListAdmissionsAsync(string? status, CancellationToken cancellationToken = default);
    Task<AdmissionResponse?> GetAdmissionAsync(int admissionId, CancellationToken cancellationToken = default);
    Task<AdmissionResponse> AdmitAsync(int patientId, int bedId, CancellationToken cancellationToken = default);
    Task<AdmissionResponse> TransferAsync(int admissionId, int newBedId, CancellationToken cancellationToken = default);
    Task<AdmissionResponse> FreeBedAsync(int admissionId, CancellationToken cancellationToken = default);
}
