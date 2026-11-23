using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Common;
using SmartCare.Application.Lab;
using SmartCare.Application.Security;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Lab;

[Authorize(Roles = Roles.LabStaff)]
public class ProcessModel : AppPageModel
{
    private readonly LabService _lab;
    private readonly IAuditService _audit;

    public ProcessModel(LabService lab, IAuditService audit)
    {
        _lab = lab;
        _audit = audit;
    }

    public LabOrder? Order { get; private set; }
    public List<ImagingStudy> Studies { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Order = await _lab.GetOrderAsync(id);
        if (Order is null) return RedirectToPage("Index");
        Studies = await _lab.GetImagingStudiesAsync(id);
        await _audit.LogReadAsync(Order.PatientId, nameof(LabOrder), Order.Id.ToString());   // PHI was viewed
        return Page();
    }

    public async Task<IActionResult> OnPostCollectAsync(Guid id)
    {
        await TryAsync(async () => { await _lab.CollectSampleAsync(id); }, "Sample collected.");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAdvanceAsync(Guid id, Guid sampleId)
    {
        if (!User.IsInRole(Roles.LabTechnician)) return Forbid();
        await TryAsync(async () => { await _lab.AdvanceSampleAsync(sampleId); }, "Sample moved to the next step.");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostResultAsync(Guid id, string? value)
    {
        if (!User.IsInRole(Roles.LabTechnician)) return Forbid();
        await TryAsync(async () => { await _lab.EnterResultAsync(id, value ?? ""); }, "Result saved. A second technician must verify it before it is released.");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostVerifyAsync(Guid id, Guid resultId)
    {
        if (!User.IsInRole(Roles.LabTechnician)) return Forbid();
        await TryAsync(async () => { await _lab.VerifyAndReleaseAsync(resultId); }, "Result verified and released. The doctor has been notified and billing informed.");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostImagingAsync(Guid id, IFormFile? file, string? dicomLink, string? reportText)
    {
        if (!User.IsInRole(Roles.LabTechnician)) return Forbid();
        await TryAsync(async () =>
        {
            if (file is { Length: > 0 })
            {
                await using var stream = file.OpenReadStream();
                await _lab.AddImagingStudyAsync(id, stream, file.FileName, file.Length, dicomLink, reportText);
            }
            else
            {
                await _lab.AddImagingStudyAsync(id, null, null, 0, dicomLink, reportText);
            }
        }, "Attached to the order and the patient record.");
        return RedirectToPage(new { id });
    }
}
