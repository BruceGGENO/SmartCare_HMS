using SmartCare.Application.DTOs.Responses;

namespace SmartCare.Application.Interfaces.Services;

public interface IWaitingService
{
    Task<IReadOnlyList<WaitingQueueItemResponse>> ListQueueAsync(string? department, string? station, CancellationToken cancellationToken = default);
    Task<WaitingQueueItemResponse> EnterQueueAsync(int appointmentId, string station, CancellationToken cancellationToken = default);
    Task<WaitingQueueItemResponse> CallAsync(int appointmentId, CancellationToken cancellationToken = default);
}
