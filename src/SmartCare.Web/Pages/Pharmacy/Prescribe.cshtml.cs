using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Common;
using SmartCare.Application.Pharmacy;
using SmartCare.Application.Security;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Pharmacy;

[Authorize(Roles = Roles.Doctor)]
public class PrescribeModel : AppPageModel
{
    private readonly PharmacyService _pharmacy;
    private readonly ClinicalLookup _lookup;

    public PrescribeModel(PharmacyService pharmacy, ClinicalLookup lookup)
    {
        _pharmacy = pharmacy;
        _lookup = lookup;
    }

    public List<Encounter> Encounters { get; private set; } = new();
    public List<Drug> Drugs { get; private set; } = new();

    [BindProperty] public Guid EncounterId { get; set; }
    [BindProperty] public string? OverrideReason { get; set; }
    [BindProperty] public List<PrescriptionItem> Rows { get; set; } = new() { new(), new(), new() };

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (await TryAsync(async () => { await _pharmacy.CreatePrescriptionAsync(EncounterId, Rows, OverrideReason); },
                "Prescription saved. It is now in the pharmacy queue."))
            return RedirectToPage();

        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Encounters = await _lookup.GetOpenEncountersAsync();
        Drugs = await _pharmacy.GetDrugsAsync();
    }
}
