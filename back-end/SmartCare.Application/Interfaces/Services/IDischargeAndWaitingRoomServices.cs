using SmartCare.Application.DTOs;

namespace SmartCare.Application.Interfaces.Services;

public interface IDischargeService
{
    Task<IReadOnlyList<DischargeDto>> GetActiveDischargesAsync(CancellationToken cancellationToken = default);
    Task<DischargeDto?> GetDischargeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DischargeDto> InitiateDischargeAsync(InitiateDischargeRequest request, Guid doctorId, CancellationToken cancellationToken = default);
    Task<bool> FinalizeDischargeAsync(Guid dischargeId, CancellationToken cancellationToken = default);
    Task<bool> MarkClearanceAsync(Guid clearanceItemId, string status, string? blockReason, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> AddAddendumAsync(Guid dischargeId, string text, Guid staffId, CancellationToken cancellationToken = default);
}

public interface IWaitingRoomService
{
    Task<IReadOnlyList<PatientJourneyEventDto>> GetLiveQueueAsync(string? station = null, CancellationToken cancellationToken = default);
    Task<PatientJourneyEventDto> RecordEventAsync(RecordJourneyEventRequest request, Guid? userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StationWaitAnalyticsDto>> GetAnalyticsAsync(DateTime? from, DateTime? to, double targetMinutes = 30, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HeatmapCellDto>> GetHeatmapAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
    Task<bool> CorrectEventAsync(CorrectJourneyEventRequest request, Guid? userId, CancellationToken cancellationToken = default);
}
