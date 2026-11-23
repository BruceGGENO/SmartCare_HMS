using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Common;
using SmartCare.Application.Lab;
using SmartCare.Application.Security;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.Lab;

[Authorize(Roles = Roles.Doctor)]
public class ResultsModel : AppPageModel
{
    private readonly LabService _lab;
    private readonly IAuditService _audit;

    public ResultsModel(LabService lab, IAuditService audit)
    {
        _lab = lab;
        _audit = audit;
    }

    public List<LabResult> Results { get; private set; } = new();
    public List<Notification> Notifications { get; private set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAcknowledgeAsync(Guid notificationId)
    {
        await TryAsync(async () => { await _lab.AcknowledgeAsync(notificationId); }, "Thank you. The critical value is acknowledged.");
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Notifications = await _lab.GetNotificationsAsync();
        Results = await _lab.GetReleasedResultsAsync();
        foreach (var patientId in Results.Select(r => r.PatientId).Distinct())
            await _audit.LogReadAsync(patientId, nameof(LabResult), null);                  // PHI was viewed
    }
}
