using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Common;
using SmartCare.Application.Lab;
using SmartCare.Application.Security;

namespace SmartCare.Web.Pages.Lab;

/// <summary>Uploaded files are never public. This page checks the role, records the access and then streams the file.</summary>
[Authorize(Roles = Roles.LabStaff + "," + Roles.Doctor)]
public class FileModel : AppPageModel
{
    private readonly LabService _lab;
    private readonly IAuditService _audit;

    public FileModel(LabService lab, IAuditService audit)
    {
        _lab = lab;
        _audit = audit;
    }

    public async Task<IActionResult> OnGetAsync(Guid studyId)
    {
        var study = await _lab.GetImagingStudyAsync(studyId);
        if (study?.FilePath is null) return NotFound();

        await _audit.LogReadAsync(study.PatientId, "ImagingStudy", study.Id.ToString());
        var stream = await _lab.OpenFileAsync(study.FilePath);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(stream, LabService.ContentTypeFor(study.FilePath));
    }
}
