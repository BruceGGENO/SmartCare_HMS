using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Common;
using SmartCare.Application.Discharge;
using SmartCare.Application.Security;
using SmartCare.Domain.Common;

namespace SmartCare.Web.Pages.Discharge;

[Authorize(Roles = Roles.Doctor + "," + Roles.Nurse + "," + Roles.HospitalAdmin + "," +
                   Roles.SuperAdmin + "," + Roles.BillingStaff + "," + Roles.Pharmacist + "," +
                   Roles.LabTechnician)]
public class DetailModel : AppPageModel
{
    private readonly DischargeService _svc;
    private readonly ICurrentUser _currentUser;

    public DetailModel(DischargeService svc, ICurrentUser currentUser)
    {
        _svc = svc;
        _currentUser = currentUser;
    }

    public SmartCare.Domain.Entities.Discharge? Discharge { get; private set; }

    public async Task OnGetAsync(Guid id)
        => Discharge = await _svc.GetDischargeAsync(id);

    // FR-DIS-002: Doctor finalises draft → creates clearance checklist
    public async Task<IActionResult> OnPostFinalizeAsync(Guid dischargeId)
    {
        await TryAsync(() => _svc.FinalizeAsync(dischargeId),
            "Discharge finalised — clearance checklist created.");
        return RedirectToPage(new { id = dischargeId });
    }

    // FR-DIS-004: Department clears its item
    public async Task<IActionResult> OnPostClearAsync(Guid clearanceItemId, Guid dischargeId)
    {
        await TryAsync(() => _svc.MarkClearanceAsync(clearanceItemId, ClearanceStatus.Cleared, null),
            "Clearance marked as cleared.");
        return RedirectToPage(new { id = dischargeId });
    }

    // FR-DIS-004: Department blocks its item
    public async Task<IActionResult> OnPostBlockAsync(Guid clearanceItemId, Guid dischargeId, string blockReason)
    {
        await TryAsync(() => _svc.MarkClearanceAsync(clearanceItemId, ClearanceStatus.Blocked, blockReason),
            "Clearance blocked.");
        return RedirectToPage(new { id = dischargeId });
    }

    // Post-finalisation addendum
    public async Task<IActionResult> OnPostAddendumAsync(Guid dischargeId, string text)
    {
        var staffId = _currentUser.StaffId ?? Guid.Empty;
        await TryAsync(() => _svc.AddAddendumAsync(dischargeId, text, staffId), "Addendum saved.");
        return RedirectToPage(new { id = dischargeId });
    }
}
