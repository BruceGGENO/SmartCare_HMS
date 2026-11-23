using Microsoft.AspNetCore.Authorization;
using SmartCare.Application.Lab;
using SmartCare.Application.Security;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Lab;

[Authorize(Roles = Roles.LabStaff)]
public class IndexModel : AppPageModel
{
    private readonly LabService _lab;
    public IndexModel(LabService lab) => _lab = lab;

    public List<LabOrder> Orders { get; private set; } = new();

    public async Task OnGetAsync() => Orders = await _lab.GetWorklistAsync();
}
