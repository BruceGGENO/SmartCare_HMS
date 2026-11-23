using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Common;
using SmartCare.Application.Lab;
using SmartCare.Application.Security;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Lab;

[Authorize(Roles = Roles.Doctor)]
public class OrderModel : AppPageModel
{
    private readonly LabService _lab;
    private readonly ClinicalLookup _lookup;

    public OrderModel(LabService lab, ClinicalLookup lookup)
    {
        _lab = lab;
        _lookup = lookup;
    }

    public List<Encounter> Encounters { get; private set; } = new();
    public List<TestCatalogItem> Tests { get; private set; } = new();
    [BindProperty] public Guid EncounterId { get; set; }
    [BindProperty] public Guid TestId { get; set; }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (await TryAsync(async () => { await _lab.OrderTestAsync(EncounterId, TestId); }, "Test ordered. It is now in the lab worklist."))
            return RedirectToPage();
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Encounters = await _lookup.GetOpenEncountersAsync();
        Tests = await _lab.GetTestsAsync();
    }
}
