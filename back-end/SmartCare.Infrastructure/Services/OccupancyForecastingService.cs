using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;

namespace SmartCare.Infrastructure.Services;

public sealed class OccupancyForecastingService : IOccupancyForecastingService
{
    private readonly ISqlExecutor _sql;
    private static readonly List<OccupancySnapshotDto> _snapshots = new();
    private static readonly object _lock = new();

    static OccupancyForecastingService()
    {
        var wards = new (string Name, int TotalBeds, double BaseOcc)[]
        {
            ("Intensive Care Unit (ICU)", 12, 0.88), // High occupancy / surge prone
            ("Surgical Ward", 30, 0.74),
            ("General Medicine", 45, 0.82),
            ("Maternity Ward", 20, 0.65),
            ("Pediatrics", 25, 0.60)
        };

        var today = DateTime.UtcNow.Date;
        for (int i = 90; i >= 1; i--)
        {
            var date = today.AddDays(-i);
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            foreach (var ward in wards)
            {
                var variation = Math.Sin(i * 0.15) * 0.08 + (isWeekend ? -0.05 : 0.04);
                var rate = Math.Clamp(ward.BaseOcc + variation, 0.45, 0.98);
                var occupied = (int)Math.Round(ward.TotalBeds * rate);

                _snapshots.Add(new OccupancySnapshotDto
                {
                    Id = Guid.NewGuid(),
                    WardName = ward.Name,
                    SnapshotDate = date,
                    OccupiedBeds = occupied,
                    TotalBeds = ward.TotalBeds
                });
            }
        }
    }

    public OccupancyForecastingService(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public Task<IReadOnlyList<OccupancySnapshotDto>> GetHistoricalSnapshotsAsync(string? ward = null, int days = 90, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-days);
        var list = _snapshots.Where(s => s.SnapshotDate >= cutoff);

        if (!string.IsNullOrWhiteSpace(ward))
            list = list.Where(s => s.WardName.Contains(ward, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult<IReadOnlyList<OccupancySnapshotDto>>(list.OrderByDescending(s => s.SnapshotDate).ToList());
    }

    public Task<OccupancySnapshotDto> TriggerDailySnapshotAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var today = DateTime.UtcNow.Date;
            var snapshot = new OccupancySnapshotDto
            {
                Id = Guid.NewGuid(),
                WardName = "Hospital-Wide Aggregate",
                SnapshotDate = today,
                OccupiedBeds = 108,
                TotalBeds = 132
            };

            _snapshots.Insert(0, snapshot);
            return Task.FromResult(snapshot);
        }
    }

    public Task<IReadOnlyList<OccupancyForecastDto>> GetForecastAsync(string? ward = null, int horizonDays = 14, CancellationToken cancellationToken = default)
    {
        var targetWards = new[] { "Intensive Care Unit (ICU)", "General Medicine", "Surgical Ward", "Maternity Ward" };
        if (!string.IsNullOrWhiteSpace(ward))
            targetWards = targetWards.Where(w => w.Contains(ward, StringComparison.OrdinalIgnoreCase)).ToArray();

        var today = DateTime.UtcNow.Date;
        var forecasts = new List<OccupancyForecastDto>();

        foreach (var w in targetWards)
        {
            var recent = _snapshots.Where(s => s.WardName == w).Take(28).ToList();
            var avgOcc = recent.Count > 0 ? recent.Average(s => s.OccupancyPercentage) : 75.0;

            for (int d = 1; d <= horizonDays; d++)
            {
                var fDate = today.AddDays(d);
                var isWeekend = fDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                var dayFactor = isWeekend ? -4.5 : 3.2;
                var waveFactor = Math.Sin(d * 0.4) * 5.0;
                var predicted = Math.Clamp(Math.Round(avgOcc + dayFactor + waveFactor, 1), 50.0, 96.0);

                forecasts.Add(new OccupancyForecastDto
                {
                    WardName = w,
                    ForecastDate = fDate,
                    DayOfWeek = fDate.DayOfWeek.ToString(),
                    PredictedOccupancyPercent = predicted,
                    LowerBoundPercent = Math.Round(Math.Max(40.0, predicted - 4.5), 1),
                    UpperBoundPercent = Math.Round(Math.Min(100.0, predicted + 5.5), 1),
                    Method = "Weekday Moving Average Heuristic",
                    ModelVersion = "v1.2-baseline"
                });
            }
        }

        return Task.FromResult<IReadOnlyList<OccupancyForecastDto>>(forecasts);
    }

    public async Task<IReadOnlyList<SurgeAlertDto>> GetSurgeAlertsAsync(CancellationToken cancellationToken = default)
    {
        var forecasts = await GetForecastAsync(horizonDays: 7, cancellationToken: cancellationToken);
        var surgeAlerts = forecasts
            .Where(f => f.IsSurgeAlert)
            .Select(f => new SurgeAlertDto
            {
                WardName = f.WardName,
                ForecastDate = f.ForecastDate,
                PredictedOccupancyPercent = f.PredictedOccupancyPercent,
                ThresholdPercent = 85.0,
                RecommendedAction = f.PredictedOccupancyPercent > 92.0
                    ? "CRITICAL: Restrict elective admissions, activate on-call nursing roster, accelerate pending discharges."
                    : "WARNING: High occupancy forecast. Alert unit charge nurse and review step-down candidates.",
                AlertGeneratedAtUtc = DateTimeOffset.UtcNow
            })
            .OrderBy(a => a.ForecastDate)
            .ToList();

        return surgeAlerts;
    }
}
