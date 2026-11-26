using SmartCare.Application.DTOs;

namespace SmartCare.Application.Interfaces.Services;

public interface IOccupancyForecastingService
{
    Task<IReadOnlyList<OccupancySnapshotDto>> GetHistoricalSnapshotsAsync(string? ward = null, int days = 90, CancellationToken cancellationToken = default);
    Task<OccupancySnapshotDto> TriggerDailySnapshotAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OccupancyForecastDto>> GetForecastAsync(string? ward = null, int horizonDays = 14, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SurgeAlertDto>> GetSurgeAlertsAsync(CancellationToken cancellationToken = default);
}
