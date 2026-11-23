using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Pharmacy;
using SmartCare.Application.Security;
using SmartCare.Domain.Common;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Pharmacy;

[Authorize(Roles = Roles.PharmacyAccess)]
public class PurchaseOrderModel : AppPageModel
{
    private readonly PharmacyService _pharmacy;
    public PurchaseOrderModel(PharmacyService pharmacy) => _pharmacy = pharmacy;

    public PurchaseOrder? Order { get; private set; }
    public List<Drug> Drugs { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        await LoadAsync(id);
        return Order is null ? RedirectToPage("PurchaseOrders") : Page();
    }

    public async Task<IActionResult> OnPostAddLineAsync(Guid id, Guid drugId, int quantity, decimal unitCost)
    {
        await TryAsync(async () => { await _pharmacy.AddPurchaseOrderLineAsync(id, drugId, quantity, unitCost); }, "Line added.");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSendAsync(Guid id)
    {
        await TryAsync(async () => { await _pharmacy.SendPurchaseOrderAsync(id); }, "Purchase order sent.");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReceiveAsync(Guid id, List<ReceiptLine> receipts)
    {
        await TryAsync(async () => { await _pharmacy.ReceivePurchaseOrderAsync(id, receipts ?? new()); }, "Goods received and stock updated.");
        return RedirectToPage(new { id });
    }

    private async Task LoadAsync(Guid id)
    {
        Order = await _pharmacy.GetPurchaseOrderAsync(id);
        Drugs = await _pharmacy.GetDrugsAsync();
    }
}
