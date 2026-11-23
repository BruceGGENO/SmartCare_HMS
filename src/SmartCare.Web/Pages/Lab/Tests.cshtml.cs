using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Common;
using SmartCare.Application.Lab;
using SmartCare.Application.Security;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Lab;

[Authorize(Roles = Roles.LabTechnician + "," + Roles.HospitalAdmin + "," + Roles.SuperAdmin)]
public class TestsModel : AppPageModel
{
    private readonly LabService _lab;
    private readonly ClinicalLookup _lookup;

    public TestsModel(LabService lab, ClinicalLookup lookup)
    {
        _lab = lab;
        _lookup = lookup;
    }

    public List<TestCatalogItem> Tests { get; private set; } = new();
    public List<PriceItem> PriceItems { get; private set; } = new();
    [BindProperty] public TestCatalogItem Input { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (await TryAsync(async () => { await _lab.CreateTestAsync(Input); }, "Test saved."))
            return RedirectToPage();
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Tests = await _lab.GetTestsAsync();
        PriceItems = await _lookup.GetPriceItemsAsync();
    }
}
