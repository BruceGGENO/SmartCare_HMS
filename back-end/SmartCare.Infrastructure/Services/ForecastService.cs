using System.Data.Common;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Services;

public sealed class ForecastService : IForecastService
{
    private readonly ISqlExecutor _sql;
    public ForecastService(ISqlExecutor sql) => _sql = sql;

    public Task<IReadOnlyList<OccupancyForecastPointResponse>> GetOccupancyAsync(int days, CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_GetOccupancyForecast", Map,
            new Dictionary<string, object?> { ["Days"] = days }, cancellationToken);

    private static OccupancyForecastPointResponse Map(DbDataReader r)
    {
        var raw = r.GetValue(r.GetOrdinal("ForecastDate"));
        var date = raw switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            _ => DateOnly.FromDateTime(Convert.ToDateTime(raw))
        };

        return new OccupancyForecastPointResponse
        {
            ForecastDate = date,
            TotalBeds = r.Get<int>("TotalBeds"),
            CurrentOccupiedBeds = r.Get<int>("CurrentOccupiedBeds"),
            PredictedOccupiedBeds = r.Get<int>("PredictedOccupiedBeds"),
            PredictedOccupancyPercent = r.Get<decimal>("PredictedOccupancyPercent"),
            Method = r.Get<string>("Method")
        };
    }
}
