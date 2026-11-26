using SmartCare.Application.DTOs;

namespace SmartCare.Application.Interfaces.Services;

public interface ILabService
{
    Task<IReadOnlyList<LabCatalogItemDto>> GetCatalogAsync(string? category = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabOrderDto>> GetOrdersAsync(string? status = null, Guid? patientId = null, CancellationToken cancellationToken = default);
    Task<LabOrderDto> CreateOrderAsync(CreateLabOrderRequest request, string orderedBy, CancellationToken cancellationToken = default);
    Task<LabOrderDto> UpdateSampleStatusAsync(Guid orderId, UpdateSampleStatusRequest request, CancellationToken cancellationToken = default);
    Task<LabOrderDto> SubmitResultAsync(Guid orderId, SubmitLabResultRequest request, string technicianName, CancellationToken cancellationToken = default);
    Task<LabOrderDto> VerifyResultAsync(Guid orderId, string verifierName, CancellationToken cancellationToken = default);
}
