using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Common;
using SmartCare.Application.Pharmacy;
using SmartCare.Application.Security;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Pharmacy;

[Authorize(Roles = Roles.Pharmacist)]
public class DispenseModel : AppPageModel
{
    private readonly PharmacyService _pharmacy;
    private readonly IAuditService _audit;

    public DispenseModel(PharmacyService pharmacy, IAuditService audit)
    {
        _pharmacy = pharmacy;
        _audit = audit;
    }

    public PrescriptionItem? Item { get; private set; }
    public List<AllergyConflict> Conflicts { get; private set; } = new();
    [BindProperty] public int Quantity { get; set; }
    [BindProperty] public string? OverrideReason { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid itemId)
    {
        if (!await LoadAsync(itemId)) return RedirectToPage("Index");
        Quantity = Item!.QuantityPrescribed - Item.QuantityDispensed;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid itemId)
    {
        if (await TryAsync(async () => { await _pharmacy.DispenseAsync(itemId, Quantity, OverrideReason); },
                "Medicine dispensed and stock updated."))
            return RedirectToPage("Index");

        if (!await LoadAsync(itemId)) return RedirectToPage("Index");
        return Page();
    }

    private async Task<bool> LoadAsync(Guid itemId)
    {
        Item = await _pharmacy.GetPrescriptionItemAsync(itemId);
        if (Item is null) return false;
        Conflicts = await _pharmacy.FindAllergyConflictsAsync(Item.PatientId, new[] { Item.DrugId });
        await _audit.LogReadAsync(Item.PatientId, nameof(Prescription), Item.PrescriptionId.ToString());   // PHI was viewed
        return true;
    }
}
