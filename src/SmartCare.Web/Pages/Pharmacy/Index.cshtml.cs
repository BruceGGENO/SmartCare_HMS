using Microsoft.AspNetCore.Authorization;
using SmartCare.Application.Pharmacy;
using SmartCare.Application.Security;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Pharmacy;

[Authorize(Roles = Roles.PharmacyAccess)]
public class IndexModel : AppPageModel
{
    private readonly PharmacyService _pharmacy;
    public IndexModel(PharmacyService pharmacy) => _pharmacy = pharmacy;

    public List<DrugStock> Stock { get; private set; } = new();
    public List<StockBatch> NearExpiry { get; private set; } = new();
    public List<StockBatch> Expired { get; private set; } = new();
    public List<Prescription> Queue { get; private set; } = new();

    public async Task OnGetAsync()
    {
        await _pharmacy.CheckReorderAlertsAsync();
        Stock = await _pharmacy.GetStockLevelsAsync();
        NearExpiry = await _pharmacy.GetNearExpiryBatchesAsync();
        Expired = await _pharmacy.GetExpiredBatchesAsync();
        if (User.IsInRole(Roles.Pharmacist)) Queue = await _pharmacy.GetPendingPrescriptionsAsync();
    }
}
