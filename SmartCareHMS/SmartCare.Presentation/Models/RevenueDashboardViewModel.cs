using SmartCare.Application.DTOs;

namespace SmartCare.Presentation.Models;

public class RevenueDashboardViewModel
{
    public RevenueSummaryDto Summary { get; set; } = new();
    public IReadOnlyList<PackageDto> Packages { get; set; } = [];
    public IReadOnlyList<PackageUsageDto> RecentUsages { get; set; } = [];
    public PurchasePackageDto NewPurchase { get; set; } = new();
}
