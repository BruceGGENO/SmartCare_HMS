using System.Collections.Concurrent;
using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;

namespace SmartCare.Infrastructure.Services;

public sealed class DischargeService : IDischargeService
{
    private readonly ISqlExecutor _sql;
    private static readonly ConcurrentDictionary<Guid, DischargeDto> _discharges = new();
    private static readonly ConcurrentDictionary<Guid, DischargeClearanceDto> _clearances = new();

    static DischargeService()
    {
        // Seed initial demo discharges
        var d1Id = Guid.NewGuid();
        var c1 = new DischargeClearanceDto { Id = Guid.NewGuid(), DischargeId = d1Id, DepartmentName = "Nursing", Status = "Cleared", IsMandatory = true, ClearedAtUtc = DateTime.UtcNow.AddHours(-1) };
        var c2 = new DischargeClearanceDto { Id = Guid.NewGuid(), DischargeId = d1Id, DepartmentName = "Pharmacy", Status = "Cleared", IsMandatory = true, ClearedAtUtc = DateTime.UtcNow.AddMinutes(-30) };
        var c3 = new DischargeClearanceDto { Id = Guid.NewGuid(), DischargeId = d1Id, DepartmentName = "Billing", Status = "Pending", IsMandatory = true };
        var c4 = new DischargeClearanceDto { Id = Guid.NewGuid(), DischargeId = d1Id, DepartmentName = "Laboratory", Status = "Cleared", IsMandatory = false, ClearedAtUtc = DateTime.UtcNow.AddHours(-2) };

        _clearances[c1.Id] = c1;
        _clearances[c2.Id] = c2;
        _clearances[c3.Id] = c3;
        _clearances[c4.Id] = c4;

        _discharges[d1Id] = new DischargeDto
        {
            Id = d1Id,
            AdmissionId = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            PatientName = "Eric Mugabe",
            PatientMrn = "MRN-2026-0012",
            DoctorId = Guid.NewGuid(),
            Stage = "ClearancesPending",
            Summary = "Patient treated for acute respiratory infection. Vitals stabilized. Cleared for discharge after final medication counseling.",
            Medications = "Amoxicillin 500mg TID x 5 days, Paracetamol 1g PRN",
            FollowUpInstructions = "Follow-up in Outpatient clinic in 7 days. Return if fever > 38.5C.",
            FinalizedAtUtc = DateTime.UtcNow.AddHours(-3),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-4),
            Clearances = [c1, c2, c3, c4]
        };

        var d2Id = Guid.NewGuid();
        var c21 = new DischargeClearanceDto { Id = Guid.NewGuid(), DischargeId = d2Id, DepartmentName = "Nursing", Status = "Pending", IsMandatory = true };
        var c22 = new DischargeClearanceDto { Id = Guid.NewGuid(), DischargeId = d2Id, DepartmentName = "Pharmacy", Status = "Pending", IsMandatory = true };
        var c23 = new DischargeClearanceDto { Id = Guid.NewGuid(), DischargeId = d2Id, DepartmentName = "Billing", Status = "Pending", IsMandatory = true };
        _clearances[c21.Id] = c21;
        _clearances[c22.Id] = c22;
        _clearances[c23.Id] = c23;

        _discharges[d2Id] = new DischargeDto
        {
            Id = d2Id,
            AdmissionId = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            PatientName = "Claire Umutoni",
            PatientMrn = "MRN-2026-0034",
            DoctorId = Guid.NewGuid(),
            Stage = "ClearancesPending",
            Summary = "Post-surgical recovery complete. Wound healing without signs of infection.",
            Medications = "Cefuroxime 250mg BID, Ibuprofen 400mg TID with food",
            FollowUpInstructions = "Suture removal in 10 days.",
            FinalizedAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            Clearances = [c21, c22, c23]
        };
    }

    public DischargeService(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public Task<IReadOnlyList<DischargeDto>> GetActiveDischargesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var d in _discharges.Values)
        {
            d.Clearances = _clearances.Values.Where(c => c.DischargeId == d.Id).ToList();
        }
        var list = _discharges.Values.OrderByDescending(d => d.CreatedAtUtc).ToList();
        return Task.FromResult<IReadOnlyList<DischargeDto>>(list);
    }

    public Task<DischargeDto?> GetDischargeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_discharges.TryGetValue(id, out var d))
        {
            d.Clearances = _clearances.Values.Where(c => c.DischargeId == d.Id).ToList();
            return Task.FromResult<DischargeDto?>(d);
        }
        return Task.FromResult<DischargeDto?>(null);
    }

    public Task<DischargeDto> InitiateDischargeAsync(InitiateDischargeRequest request, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var discharge = new DischargeDto
        {
            Id = id,
            AdmissionId = request.AdmissionId != Guid.Empty ? request.AdmissionId : Guid.NewGuid(),
            PatientId = request.PatientId != Guid.Empty ? request.PatientId : Guid.NewGuid(),
            PatientName = string.IsNullOrWhiteSpace(request.PatientName) ? "Patient" : request.PatientName.Trim(),
            PatientMrn = string.IsNullOrWhiteSpace(request.PatientMrn) ? "MRN-DEMO" : request.PatientMrn.Trim(),
            DoctorId = doctorId,
            Stage = "Draft",
            Summary = request.Summary.Trim(),
            Medications = request.Medications?.Trim(),
            FollowUpInstructions = request.FollowUpInstructions?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _discharges[id] = discharge;
        return Task.FromResult(discharge);
    }

    public Task<bool> FinalizeDischargeAsync(Guid dischargeId, CancellationToken cancellationToken = default)
    {
        if (!_discharges.TryGetValue(dischargeId, out var d)) return Task.FromResult(false);

        d.Stage = "ClearancesPending";
        d.FinalizedAtUtc = DateTime.UtcNow;

        var depts = new[]
        {
            (Name: "Nursing", Mandatory: true),
            (Name: "Pharmacy", Mandatory: true),
            (Name: "Billing", Mandatory: true),
            (Name: "Laboratory", Mandatory: false)
        };

        foreach (var (name, mandatory) in depts)
        {
            var cid = Guid.NewGuid();
            var item = new DischargeClearanceDto
            {
                Id = cid,
                DischargeId = dischargeId,
                DepartmentName = name,
                Status = "Pending",
                IsMandatory = mandatory
            };
            _clearances[cid] = item;
            d.Clearances.Add(item);
        }

        return Task.FromResult(true);
    }

    public Task<bool> MarkClearanceAsync(Guid clearanceItemId, string status, string? blockReason, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_clearances.TryGetValue(clearanceItemId, out var c)) return Task.FromResult(false);

        c.Status = status;
        c.BlockReason = status == "Blocked" ? blockReason : null;
        c.ClearedAtUtc = status == "Cleared" ? DateTime.UtcNow : null;

        // Auto-complete if all mandatory clearances cleared
        if (_discharges.TryGetValue(c.DischargeId, out var d))
        {
            var allClearances = _clearances.Values.Where(x => x.DischargeId == d.Id).ToList();
            if (allClearances.Where(x => x.IsMandatory).All(x => x.Status == "Cleared"))
            {
                d.Stage = "Completed";
                d.CompletedAtUtc = DateTime.UtcNow;
            }
        }

        return Task.FromResult(true);
    }

    public Task<bool> AddAddendumAsync(Guid dischargeId, string text, Guid staffId, CancellationToken cancellationToken = default)
    {
        if (!_discharges.TryGetValue(dischargeId, out var d)) return Task.FromResult(false);
        d.Summary += $"\n[Addendum {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC]: {text}";
        return Task.FromResult(true);
    }
}
