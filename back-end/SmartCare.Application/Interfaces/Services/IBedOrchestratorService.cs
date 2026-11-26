using SmartCare.Application.DTOs;

namespace SmartCare.Application.Interfaces.Services;

public interface IBedOrchestratorService
{
    Task<IReadOnlyList<BedDto>> GetBedInventoryAsync(string? ward = null, string? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BedTransferRequestDto>> GetTransferRequestsAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<BedTransferRequestDto> RequestTransferAsync(CreateBedTransferRequest request, string requestedBy, CancellationToken cancellationToken = default);
    Task<BedTransferRequestDto> AssignBedAsync(Guid requestId, Guid bedId, string assignedBy, CancellationToken cancellationToken = default);
    Task<BedTransferRequestDto> UpdateTransferStageAsync(Guid requestId, string stage, string updatedBy, CancellationToken cancellationToken = default);
    Task<BedDto> UpdateBedStatusAsync(Guid bedId, string newStatus, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BedTurnaroundReportDto>> GetTurnaroundReportAsync(CancellationToken cancellationToken = default);
}
