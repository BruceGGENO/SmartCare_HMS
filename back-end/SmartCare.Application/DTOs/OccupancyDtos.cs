namespace SmartCare.Application.DTOs;

public sealed class OccupancySnapshotDto
{
    public Guid Id { get; set; }
    public string WardName { get; set; } = string.Empty;
    public DateTime SnapshotDate { get; set; }
    public int OccupiedBeds { get; set; }
    public int TotalBeds { get; set; }
    public double OccupancyPercentage => TotalBeds > 0 ? Math.Round((double)OccupiedBeds / TotalBeds * 100, 1) : 0;
}

public sealed class OccupancyForecastDto
{
    public string WardName { get; set; } = string.Empty;
    public DateTime ForecastDate { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public double PredictedOccupancyPercent { get; set; }
    public double LowerBoundPercent { get; set; }
    public double UpperBoundPercent { get; set; }
    public string Method { get; set; } = "Weekday Moving Average Heuristic";
    public string ModelVersion { get; set; } = "v1.2-baseline";
    public bool IsSurgeAlert => PredictedOccupancyPercent >= 85.0;
}

public sealed class SurgeAlertDto
{
    public string WardName { get; set; } = string.Empty;
    public DateTime ForecastDate { get; set; }
    public double PredictedOccupancyPercent { get; set; }
    public double ThresholdPercent { get; set; } = 85.0;
    public string RecommendedAction { get; set; } = string.Empty;
    public DateTimeOffset AlertGeneratedAtUtc { get; set; }
}
