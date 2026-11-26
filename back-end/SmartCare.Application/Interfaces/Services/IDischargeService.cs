using SmartCare.Application.DTOs.Responses;

namespace SmartCare.Application.Interfaces.Services;

public interface IDischargeService
{
    Task<IReadOnlyList<DischargeCaseResponse>> ListAsync(string? status, CancellationToken cancellationToken = default);
    Task<DischargeCaseResponse?> GetByIdAsync(int dischargeId, CancellationToken cancellationToken = default);
    Task<DischargeCaseResponse> InitiateAsync(int admissionId, CancellationToken cancellationToken = default);
    Task<DischargeStepResponse> CompleteStepAsync(int dischargeStepId, CancellationToken cancellationToken = default);
    Task<DischargeCaseResponse> CompleteAsync(int dischargeId, CancellationToken cancellationToken = default);
}
