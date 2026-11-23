using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Pharmacy;
using SmartCare.Application.Security;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Pharmacy;

[Authorize(Roles = Roles.PharmacyAccess)]
public class PurchaseOrdersModel : AppPageModel
{
    private readonly PharmacyService _pharmacy;
    public PurchaseOrdersModel(PharmacyService pharmacy) => _pharmacy = pharmacy;

    public List<PurchaseOrder> Orders { get; private set; } = new();
    public List<Supplier> Suppliers { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Orders = await _pharmacy.GetPurchaseOrdersAsync();
        Suppliers = await _pharmacy.GetSuppliersAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(Guid supplierId)
    {
        Guid? id = null;
        if (await TryAsync(async () => { id = (await _pharmacy.CreatePurchaseOrderAsync(supplierId)).Id; }))
            return RedirectToPage("PurchaseOrder", new { id });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSupplierAsync(string name, string? phone, string? email)
    {
        await TryAsync(async () => { await _pharmacy.CreateSupplierAsync(name ?? "", phone, email); }, "Supplier saved.");
        return RedirectToPage();
    }
}
