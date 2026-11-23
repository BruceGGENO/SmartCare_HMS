using Microsoft.EntityFrameworkCore;
using SmartCare.Application.Common;
using SmartCare.Domain.Common;
using SmartCare.Domain.Entities;

namespace SmartCare.Application.Discharge;

public record DepartmentDelayStats(
    string DepartmentName,
    double AvgMinutes,
    double MedianMinutes,
    double P90Minutes,
    int BlockCount,
    int TotalClearances);

public class DischargeService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _user;

    public DischargeService(IAppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    // ── FR-DIS-001: Doctor initiates a discharge in Draft ────────────────────
    public async Task<SmartCare.Domain.Entities.Discharge> InitiateAsync(
        Guid admissionId,
        Guid doctorStaffId,
        string summary,
        string? medications,
        string? followUpInstructions,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(summary))
            throw new BusinessRuleException("Discharge summary is required.");

        var admission = await _db.Set<Admission>()
            .FirstOrDefaultAsync(a => a.Id == admissionId, ct)
            ?? throw new BusinessRuleException("Admission not found.");

        if (admission.Status == AdmissionStatus.Discharged)
            throw new BusinessRuleException("This patient has already been discharged.");

        var already = await _db.Set<SmartCare.Domain.Entities.Discharge>()
            .AnyAsync(d => d.AdmissionId == admissionId && d.Stage != DischargeStage.Completed, ct);
        if (already)
            throw new BusinessRuleException("A discharge is already in progress for this admission.");

        var discharge = new SmartCare.Domain.Entities.Discharge
        {
            AdmissionId = admissionId,
            PatientId = admission.PatientId,
            DoctorId = doctorStaffId,
            Stage = DischargeStage.Draft,
            Summary = summary.Trim(),
            Medications = string.IsNullOrWhiteSpace(medications) ? null : medications.Trim(),
            FollowUpInstructions = string.IsNullOrWhiteSpace(followUpInstructions) ? null : followUpInstructions.Trim()
        };

        _db.Set<SmartCare.Domain.Entities.Discharge>().Add(discharge);
        await _db.SaveChangesAsync(ct);
        return discharge;
    }

    // ── FR-DIS-002 + FR-DIS-003: Finalize → lock content → auto-create checklist ──
    public async Task FinalizeAsync(Guid dischargeId, CancellationToken ct = default)
    {
        var discharge = await _db.Set<SmartCare.Domain.Entities.Discharge>()
            .Include(d => d.Clearances)
            .FirstOrDefaultAsync(d => d.Id == dischargeId, ct)
            ?? throw new BusinessRuleException("Discharge not found.");

        if (discharge.Stage != DischargeStage.Draft)
            throw new BusinessRuleException($"Only a Draft discharge can be finalized. Current stage: {discharge.Stage}.");

        // Lock clinical content
        discharge.FinalizedAtUtc = DateTime.UtcNow;
        discharge.Stage = DischargeStage.Final;

        // Create one clearance item per configured clearance department
        var departments = await _db.Set<Department>()
            .Where(d => d.IsDischargeClearanceDepartment)
            .ToListAsync(ct);

        foreach (var dept in departments)
        {
            discharge.Clearances.Add(new DischargeClearanceItem
            {
                DischargeId = discharge.Id,
                DepartmentId = dept.Id,
                Status = ClearanceStatus.Pending,
                IsMandatory = dept.IsMandatoryClearance
            });
        }

        discharge.Stage = DischargeStage.ClearancesPending;
        await _db.SaveChangesAsync(ct);
    }

    // ── FR-DIS-004: each department marks its clearance ──────────────────────
    public async Task MarkClearanceAsync(
        Guid clearanceItemId,
        ClearanceStatus status,
        string? blockReason,
        CancellationToken ct = default)
    {
        var userId = _user.UserId ?? throw new BusinessRuleException("You must be signed in.");

        if (status == ClearanceStatus.Blocked && string.IsNullOrWhiteSpace(blockReason))
            throw new BusinessRuleException("A reason is required when blocking a clearance.");

        var item = await _db.Set<DischargeClearanceItem>()
            .Include(i => i.Discharge)
            .FirstOrDefaultAsync(i => i.Id == clearanceItemId, ct)
            ?? throw new BusinessRuleException("Clearance item not found.");

        if (item.Discharge.Stage != DischargeStage.ClearancesPending)
            throw new BusinessRuleException("This discharge is not awaiting clearances.");

        item.Status = status;
        item.BlockReason = status == ClearanceStatus.Blocked ? blockReason!.Trim() : null;
        item.ClearedByUserId = userId;
        item.ClearedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // FR-DIS-005: auto-complete when every mandatory clearance is Cleared
        if (status == ClearanceStatus.Cleared)
            await TryCompleteAsync(item.DischargeId, ct);
    }

    // ── FR-DIS-005: complete when all mandatory clearances are done ───────────
    private async Task TryCompleteAsync(Guid dischargeId, CancellationToken ct)
    {
        var clearances = await _db.Set<DischargeClearanceItem>()
            .Where(c => c.DischargeId == dischargeId)
            .ToListAsync(ct);

        var allDone = clearances.Where(c => c.IsMandatory).All(c => c.Status == ClearanceStatus.Cleared);
        if (!allDone) return;

        var discharge = await _db.Set<SmartCare.Domain.Entities.Discharge>()
            .Include(d => d.Admission).ThenInclude(a => a.CurrentBed)
            .FirstOrDefaultAsync(d => d.Id == dischargeId, ct);
        if (discharge == null) return;

        discharge.Stage = DischargeStage.Completed;
        discharge.CompletedAtUtc = DateTime.UtcNow;

        if (discharge.Admission != null)
        {
            discharge.Admission.Status = AdmissionStatus.Discharged;
            discharge.Admission.DischargedAtUtc = DateTime.UtcNow;

            // Free the bed for cleaning
            if (discharge.Admission.CurrentBed != null)
                discharge.Admission.CurrentBed.Status = BedStatus.Cleaning;
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── FR-DIS-006: live dashboard ────────────────────────────────────────────
    public Task<List<SmartCare.Domain.Entities.Discharge>> GetActiveDischargesAsync(
        CancellationToken ct = default) =>
        _db.Set<SmartCare.Domain.Entities.Discharge>().AsNoTracking()
           .Include(d => d.Clearances).ThenInclude(c => c.Department)
           .Include(d => d.Admission).ThenInclude(a => a.Patient)
           .Include(d => d.Admission).ThenInclude(a => a.CurrentBed)
           .Where(d => d.Stage != DischargeStage.Completed)
           .OrderBy(d => d.FinalizedAtUtc)
           .ToListAsync(ct);

    public Task<SmartCare.Domain.Entities.Discharge?> GetDischargeAsync(
        Guid id, CancellationToken ct = default) =>
        _db.Set<SmartCare.Domain.Entities.Discharge>().AsNoTracking()
           .Include(d => d.Clearances).ThenInclude(c => c.Department)
           .Include(d => d.Addenda)
           .Include(d => d.Admission).ThenInclude(a => a.Patient)
           .Include(d => d.Admission).ThenInclude(a => a.CurrentBed)
           .FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<List<SmartCare.Domain.Entities.Discharge>> GetAllAsync(
        CancellationToken ct = default) =>
        _db.Set<SmartCare.Domain.Entities.Discharge>().AsNoTracking()
           .Include(d => d.Clearances).ThenInclude(c => c.Department)
           .Include(d => d.Admission).ThenInclude(a => a.Patient)
           .OrderByDescending(d => d.CreatedAtUtc)
           .ToListAsync(ct);

    // ── Addendum (after finalization) ─────────────────────────────────────────
    public async Task<DischargeAddendum> AddAddendumAsync(
        Guid dischargeId, string text, Guid authorStaffId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new BusinessRuleException("Addendum text is required.");

        var discharge = await _db.Set<SmartCare.Domain.Entities.Discharge>()
            .FirstOrDefaultAsync(d => d.Id == dischargeId, ct)
            ?? throw new BusinessRuleException("Discharge not found.");
        if (discharge.Stage == DischargeStage.Draft)
            throw new BusinessRuleException("Addenda can only be added after finalization.");

        var addendum = new DischargeAddendum
        {
            DischargeId = dischargeId,
            PatientId = discharge.PatientId,
            Text = text.Trim(),
            AuthorStaffId = authorStaffId
        };
        _db.Set<DischargeAddendum>().Add(addendum);
        await _db.SaveChangesAsync(ct);
        return addendum;
    }

    // ── FR-DIS-007: per-department delay analytics ────────────────────────────
    public async Task<List<DepartmentDelayStats>> GetDelayAnalyticsAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var items = await _db.Set<DischargeClearanceItem>().AsNoTracking()
            .Include(i => i.Department)
            .Where(i => i.CreatedAtUtc >= from && i.CreatedAtUtc <= to)
            .ToListAsync(ct);

        return items
            .GroupBy(i => i.Department.Name)
            .Select(g =>
            {
                var minutes = g
                    .Where(i => i.ClearedAtUtc.HasValue)
                    .Select(i => (i.ClearedAtUtc!.Value - i.CreatedAtUtc).TotalMinutes)
                    .OrderBy(m => m)
                    .ToList();

                return new DepartmentDelayStats(
                    g.Key,
                    minutes.Count > 0 ? minutes.Average() : 0,
                    minutes.Count > 0 ? Percentile(minutes, 0.5) : 0,
                    minutes.Count > 0 ? Percentile(minutes, 0.9) : 0,
                    g.Count(i => i.Status == ClearanceStatus.Blocked),
                    g.Count());
            })
            .ToList();
    }

    // ── FR-DIS-008: clearances overdue for the background reminder job ────────
    public Task<List<DischargeClearanceItem>> GetOverdueClearancesAsync(
        int thresholdMinutes, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-thresholdMinutes);
        return _db.Set<DischargeClearanceItem>().AsNoTracking()
            .Include(i => i.Department)
            .Include(i => i.Discharge).ThenInclude(d => d.Admission).ThenInclude(a => a.Patient)
            .Where(i => i.Status == ClearanceStatus.Pending
                     && i.CreatedAtUtc <= cutoff
                     && (i.LastReminderAtUtc == null || i.LastReminderAtUtc <= cutoff))
            .ToListAsync(ct);
    }

    public async Task UpdateReminderTimestampAsync(Guid clearanceItemId, CancellationToken ct = default)
    {
        var item = await _db.Set<DischargeClearanceItem>()
            .FirstOrDefaultAsync(i => i.Id == clearanceItemId, ct);
        if (item != null)
        {
            item.LastReminderAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static double Percentile(List<double> sorted, double p)
    {
        if (sorted.Count == 0) return 0;
        var idx = p * (sorted.Count - 1);
        var lower = (int)Math.Floor(idx);
        var upper = (int)Math.Ceiling(idx);
        if (lower == upper) return sorted[lower];
        return sorted[lower] + (sorted[upper] - sorted[lower]) * (idx - lower);
    }
}
