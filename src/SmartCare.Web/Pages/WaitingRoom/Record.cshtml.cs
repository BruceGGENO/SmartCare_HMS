using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCare.Application.Common;
using SmartCare.Application.Security;
using SmartCare.Application.WaitingRoom;
using SmartCare.Domain.Common;
using SmartCare.Domain.Entities;

namespace SmartCare.Web.Pages.WaitingRoom;

[Authorize(Roles = Roles.Receptionist + "," + Roles.Nurse + "," + Roles.Doctor + "," +
                   Roles.LabTechnician + "," + Roles.Pharmacist)]
public class RecordModel : AppPageModel
{
    private readonly WaitingRoomService _svc;
    private readonly IAppDbContext _db;

    public RecordModel(WaitingRoomService svc, IAppDbContext db)
    {
        _svc = svc;
        _db = db;
    }

    // For form repopulation on error
    public string? Mrn { get; set; }
    public JourneyStation SelectedStation { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string mrn, JourneyStation station,
        JourneyEventType eventType, string? appointmentId)
    {
        if (string.IsNullOrWhiteSpace(mrn))
        {
            TempData["Error"] = "MRN is required.";
            Mrn = mrn;
            SelectedStation = station;
            return Page();
        }

        // Find patient by MRN
        var patient = await _db.Set<Patient>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Mrn == mrn.Trim());

        if (patient == null)
        {
            TempData["Error"] = $"No patient found with MRN '{mrn.Trim()}'.";
            Mrn = mrn;
            SelectedStation = station;
            return Page();
        }

        Guid? apptId = null;
        if (!string.IsNullOrWhiteSpace(appointmentId) && Guid.TryParse(appointmentId.Trim(), out var parsed))
            apptId = parsed;

        var ok = await TryAsync(() =>
            _svc.RecordEventAsync(patient.Id, apptId, null, station, eventType),
            $"Recorded: {patient.FirstName} {patient.LastName} → {eventType} at {station}.");

        if (ok) return RedirectToPage("Index");

        Mrn = mrn;
        SelectedStation = station;
        return Page();
    }
}
