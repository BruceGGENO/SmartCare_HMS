using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Pharmacy;
using SmartCare.Application.Security;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Pharmacy;

[Authorize(Roles = Roles.PharmacyAccess)]
public class StockModel : AppPageModel
{
    private readonly PharmacyService _pharmacy;
    public StockModel(PharmacyService pharmacy) => _pharmacy = pharmacy;

    public List<StockBatch> Batches { get; private set; } = new();
    public List<Drug> Drugs { get; private set; } = new();
    public List<Supplier> Suppliers { get; private set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostReceiveAsync(Guid drugId, string? store, string batchNumber, DateOnly? expiry,
        int quantity, decimal unitCost, Guid? supplierId)
    {
        if (expiry is null) { TempData["Error"] = "Enter the expiry date."; return RedirectToPage(); }
        await TryAsync(async () => { await _pharmacy.ReceiveStockAsync(drugId, store ?? "", batchNumber ?? "", expiry.Value, quantity, unitCost, supplierId); },
            "Stock received.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAdjustAsync(Guid batchId, int amount, string? reason)
    {
        await TryAsync(async () => { await _pharmacy.AdjustStockAsync(batchId, amount, reason ?? ""); }, "Stock adjusted.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostWriteOffAsync(Guid batchId, int amount, string? reason)
    {
        await TryAsync(async () => { await _pharmacy.WriteOffAsync(batchId, amount, reason ?? ""); }, "Stock written off.");
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Batches = await _pharmacy.GetBatchesAsync();
        Drugs = await _pharmacy.GetDrugsAsync();
        Suppliers = await _pharmacy.GetSuppliersAsync();
    }
}
