namespace SmartCare.Application.DTOs.Responses;

public sealed class OccupancyForecastPointResponse
{
    public DateOnly ForecastDate { get; set; }
    public int TotalBeds { get; set; }
    public int CurrentOccupiedBeds { get; set; }
    public int PredictedOccupiedBeds { get; set; }
    public decimal PredictedOccupancyPercent { get; set; }
    public string Method { get; set; } = "Heuristic";
}
