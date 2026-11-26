using SmartCare.Application.DTOs.Responses;

namespace SmartCare.Application.Interfaces.Services;

public interface ILabService
{
    Task<IReadOnlyList<LabTestResponse>> ListTestsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabOrderResponse>> ListOrdersAsync(int? patientId, string? status, CancellationToken cancellationToken = default);
    Task<LabOrderResponse?> GetOrderAsync(int labOrderId, CancellationToken cancellationToken = default);
    Task<LabOrderResponse> CreateOrderAsync(int patientId, int labTestId, Guid? orderedByUserId, CancellationToken cancellationToken = default);
    Task<LabOrderResponse> FileResultAsync(int labOrderId, string resultValue, string? notes, Guid? resultedByUserId, CancellationToken cancellationToken = default);
}
