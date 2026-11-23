using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCare.Application.Common;
using SmartCare.Application.Discharge;
using SmartCare.Application.Security;
using SmartCare.Domain.Common;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Discharge;

[Authorize(Roles = Roles.Doctor)]
public class InitiateModel : AppPageModel
{
    private readonly DischargeService _svc;
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public InitiateModel(DischargeService svc, IAppDbContext db, ICurrentUser currentUser)
    {
        _svc = svc;
        _db = db;
        _currentUser = currentUser;
    }

    public List<Admission> OpenAdmissions { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Find admissions that are still Admitted and don't already have an active discharge
        var activeDischargeAdmissionIds = await _db.Set<SmartCare.Domain.Entities.Discharge>()
            .Where(d => d.Stage != DischargeStage.Completed)
            .Select(d => d.AdmissionId)
            .ToListAsync();

        OpenAdmissions = await _db.Set<Admission>()
            .AsNoTracking()
            .Include(a => a.Patient)
            .Where(a => a.Status == AdmissionStatus.Admitted
                     && !activeDischargeAdmissionIds.Contains(a.Id))
            .OrderByDescending(a => a.AdmittedAtUtc)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(Guid admissionId, string summary,
        string? medications, string? followUpInstructions)
    {
        var staffId = _currentUser.StaffId
            ?? throw new BusinessRuleException("Your account is not linked to a staff record.");

        var ok = await TryAsync(async () =>
        {
            var discharge = await _svc.InitiateAsync(admissionId, staffId, summary,
                medications, followUpInstructions);
            TempData["_dischargeId"] = discharge.Id.ToString();
        }, "Discharge initiated successfully.");

        if (ok && TempData["_dischargeId"] is string id)
        {
            TempData.Remove("_dischargeId");
            return RedirectToPage("Detail", new { id });
        }

        // Reload the form on error
        await OnGetAsync();
        return Page();
    }
}
