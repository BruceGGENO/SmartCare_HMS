using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Common;
using SmartCare.Application.Pharmacy;
using SmartCare.Application.Security;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Pharmacy;

[Authorize(Roles = Roles.PharmacyAccess)]
public class DrugsModel : AppPageModel
{
    private readonly PharmacyService _pharmacy;
    private readonly ClinicalLookup _lookup;

    public DrugsModel(PharmacyService pharmacy, ClinicalLookup lookup)
    {
        _pharmacy = pharmacy;
        _lookup = lookup;
    }

    public List<Drug> Drugs { get; private set; } = new();
    public List<PriceItem> PriceItems { get; private set; } = new();
    [BindProperty] public Drug Input { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (await TryAsync(async () => { await _pharmacy.CreateDrugAsync(Input); }, "Drug saved."))
            return RedirectToPage();
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Drugs = await _pharmacy.GetDrugsAsync();
        PriceItems = await _lookup.GetPriceItemsAsync();
    }
}
