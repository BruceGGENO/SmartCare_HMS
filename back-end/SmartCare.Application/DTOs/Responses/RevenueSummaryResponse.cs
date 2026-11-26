namespace SmartCare.Application.DTOs.Responses;

public sealed class RevenueSummaryResponse
{
    public decimal TotalCashCollected { get; set; }

    public decimal TotalEarnedRevenue { get; set; }

    public decimal TotalDeferredRevenue { get; set; }

    public int TotalPackagesCount { get; set; }

    public int ActivePackagesCount { get; set; }

    public int TotalSessionsSold { get; set; }

    public int TotalSessionsConsumed { get; set; }

    public int RemainingSessions => TotalSessionsSold - TotalSessionsConsumed;

    public int EarnedPercentage => TotalCashCollected > 0
        ? (int)Math.Round((TotalEarnedRevenue / TotalCashCollected) * 100)
        : 0;
}
