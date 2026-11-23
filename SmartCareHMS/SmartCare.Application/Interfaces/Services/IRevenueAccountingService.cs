using SmartCare.Application.DTOs;

namespace SmartCare.Application.Interfaces.Services;

public interface IRevenueAccountingService
{
    Task<RevenueSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageDto>> GetAllPackagesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageUsageDto>> GetRecentUsagesAsync(CancellationToken cancellationToken = default);

    Task<bool> PurchasePackageAsync(PurchasePackageDto dto, CancellationToken cancellationToken = default);

    Task<bool> ConsumeSessionAsync(int packageId, string? notes = null, CancellationToken cancellationToken = default);

    Task<byte[]> ExportReconciliationCsvAsync(CancellationToken cancellationToken = default);
}
