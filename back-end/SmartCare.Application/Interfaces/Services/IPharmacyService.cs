using SmartCare.Application.DTOs;

namespace SmartCare.Application.Interfaces.Services;

public interface IPharmacyService
{
    Task<IReadOnlyList<DrugCatalogDto>> GetDrugCatalogAsync(string? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockBatchDto>> GetStockBatchesAsync(Guid? drugId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReorderAlertDto>> GetReorderAlertsAsync(CancellationToken cancellationToken = default);
    Task<DispenseResultDto> DispenseMedicationAsync(DispenseMedicationRequest request, string dispensedBy, CancellationToken cancellationToken = default);
}
