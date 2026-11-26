namespace SmartCare.Application.DTOs.Responses;

public sealed class PackageBalanceResponse
{
    public int PackagePurchaseId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SessionsPurchased { get; set; }

    public int SessionsConsumed { get; set; }

    public int SessionsRemaining => Math.Max(0, SessionsPurchased - SessionsConsumed);
}
