using SmartCare.Application.DTOs.Responses;

namespace SmartCare.Application.Interfaces.Services;

public interface IForecastService
{
    Task<IReadOnlyList<OccupancyForecastPointResponse>> GetOccupancyAsync(int days, CancellationToken cancellationToken = default);
}
