using SmartCare.Application.DTOs.Responses;

namespace SmartCare.Application.Interfaces.Services;

public interface IRevenueService
{
    Task EnsureDemoDataAsync(CancellationToken cancellationToken = default);

    Task<RevenueSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrepaidPackageResponse>> ListPackagesAsync(
        int? patientId,
        CancellationToken cancellationToken = default);

    Task<PrepaidPackageResponse?> GetPackageByIdAsync(int packageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageUsageResponse>> ListRecentUsagesAsync(CancellationToken cancellationToken = default);

    Task<PrepaidPackageResponse> PurchaseAsync(
        int patientId,
        string packageName,
        int totalSessions,
        decimal totalAmount,
        CancellationToken cancellationToken = default);

    Task<PackageUsageResponse> ConsumeSessionAsync(
        int packageId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportReconciliationCsvAsync(CancellationToken cancellationToken = default);
}
