using Microsoft.AspNetCore.Authorization;
using SmartCare.Application.Discharge;
using SmartCare.Application.Security;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Discharge;

[Authorize(Roles = Roles.Doctor + "," + Roles.Nurse + "," + Roles.HospitalAdmin + "," + Roles.SuperAdmin + "," + Roles.BillingStaff + "," + Roles.Pharmacist + "," + Roles.LabTechnician)]
public class IndexModel : AppPageModel
{
    private readonly DischargeService _svc;
    public IndexModel(DischargeService svc) => _svc = svc;

    public List<SmartCare.Domain.Entities.Discharge> Discharges { get; private set; } = new();

    public async Task OnGetAsync() =>
        Discharges = await _svc.GetActiveDischargesAsync();
}
