using System.Collections.Concurrent;
using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;

namespace SmartCare.Infrastructure.Services;

public sealed class BedOrchestratorService : IBedOrchestratorService
{
    private readonly ISqlExecutor _sql;
    private static readonly ConcurrentDictionary<Guid, BedDto> _beds = new();
    private static readonly ConcurrentDictionary<Guid, BedTransferRequestDto> _transfers = new();
    private static readonly object _lock = new();

    static BedOrchestratorService()
    {
        // Seed 12 realistic hospital beds across ICU, Surgical Ward, General Medicine, and Maternity
        var b1 = new BedDto { Id = Guid.Parse("33333333-0000-0000-0000-000000000001"), WardName = "Intensive Care Unit (ICU)", RoomNumber = "ICU-01", BedNumber = "Bed 1", BedType = "ICU Ventilated", Status = "Occupied", CurrentPatientName = "Eric Mugabe", CurrentPatientMrn = "SC-0012", OccupiedSinceUtc = DateTimeOffset.UtcNow.AddDays(-2) };
        var b2 = new BedDto { Id = Guid.Parse("33333333-0000-0000-0000-000000000002"), WardName = "Intensive Care Unit (ICU)", RoomNumber = "ICU-01", BedNumber = "Bed 2", BedType = "ICU Ventilated", Status = "Available", LastCleanedAtUtc = DateTimeOffset.UtcNow.AddHours(-1) };
        var b3 = new BedDto { Id = Guid.Parse("33333333-0000-0000-0000-000000000003"), WardName = "Intensive Care Unit (ICU)", RoomNumber = "ICU-02", BedNumber = "Bed 3", BedType = "ICU Isolation", Status = "Cleaning", LastCleanedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-20) };
        var b4 = new BedDto { Id = Guid.Parse("33333333-0000-0000-0000-000000000004"), WardName = "Surgical Ward", RoomNumber = "SURG-101", BedNumber = "Bed A", BedType = "Post-Op Surgical", Status = "Available", LastCleanedAtUtc = DateTimeOffset.UtcNow.AddHours(-5) };
        var b5 = new BedDto { Id = Guid.Parse("33333333-0000-0000-0000-000000000005"), WardName = "Surgical Ward", RoomNumber = "SURG-101", BedNumber = "Bed B", BedType = "Post-Op Surgical", Status = "Occupied", CurrentPatientName = "Jean Uwimana", CurrentPatientMrn = "SC-0002", OccupiedSinceUtc = DateTimeOffset.UtcNow.AddDays(-1) };
        var b6 = new BedDto { Id = Guid.Parse("33333333-0000-0000-0000-000000000006"), WardName = "Surgical Ward", RoomNumber = "SURG-102", BedNumber = "Bed C", BedType = "Step-down", Status = "Reserved", LastCleanedAtUtc = DateTimeOffset.UtcNow.AddHours(-2) };
        var b7 = new BedDto { Id = Guid.Parse("33333333-0000-0000-0000-000000000007"), WardName = "General Medicine", RoomNumber = "MED-201", BedNumber = "Bed 1", BedType = "General Acute", Status = "Available", LastCleanedAtUtc = DateTimeOffset.UtcNow.AddHours(-3) };
        var b8 = new BedDto { Id = Guid.Parse("33333333-0000-0000-0000-000000000008"), WardName = "General Medicine", RoomNumber = "MED-201", BedNumber = "Bed 2", BedType = "General Acute", Status = "Occupied", CurrentPatientName = "Aline Mukamana", CurrentPatientMrn = "SC-0089", OccupiedSinceUtc = DateTimeOffset.UtcNow.AddHours(-18) };
        var b9 = new BedDto { Id = Guid.Parse("33333333-0000-0000-0000-000000000009"), WardName = "Maternity Ward", RoomNumber = "MAT-301", BedNumber = "Bed 1", BedType = "Labor & Delivery", Status = "Available", LastCleanedAtUtc = DateTimeOffset.UtcNow.AddHours(-4) };

        _beds[b1.Id] = b1;
        _beds[b2.Id] = b2;
        _beds[b3.Id] = b3;
        _beds[b4.Id] = b4;
        _beds[b5.Id] = b5;
        _beds[b6.Id] = b6;
        _beds[b7.Id] = b7;
        _beds[b8.Id] = b8;
        _beds[b9.Id] = b9;

        // Seed transfer requests
        var t1Id = Guid.NewGuid();
        _transfers[t1Id] = new BedTransferRequestDto
        {
            Id = t1Id,
            PatientId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            PatientName = "Demo Patient",
            PatientMrn = "SC-0001",
            FromUnit = "Operating Theatre (OT 2)",
            ToUnit = "Intensive Care Unit (ICU)",
            RequestedBedType = "ICU Ventilated",
            Priority = "STAT",
            Status = "BedAssigned",
            RequestedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-45),
            BedAssignedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-20),
            AssignedBedId = b2.Id,
            AssignedBedNumber = "ICU-01 / Bed 2",
            RequesterName = "Dr. Robert Mugabe (Anesthesiologist)",
            ClinicalNotes = "Emergency post-laparotomy. Stable on mechanical ventilation. Requires ICU monitoring x 24h."
        };
    }

    public BedOrchestratorService(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public Task<IReadOnlyList<BedDto>> GetBedInventoryAsync(string? ward = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var list = _beds.Values.ToList();
        if (!string.IsNullOrWhiteSpace(ward))
            list = list.Where(b => b.WardName.Contains(ward, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(status))
            list = list.Where(b => string.Equals(b.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();

        return Task.FromResult<IReadOnlyList<BedDto>>(list.OrderBy(b => b.WardName).ThenBy(b => b.BedNumber).ToList());
    }

    public Task<IReadOnlyList<BedTransferRequestDto>> GetTransferRequestsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var list = _transfers.Values.ToList();
        if (!string.IsNullOrWhiteSpace(status))
            list = list.Where(t => string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();

        return Task.FromResult<IReadOnlyList<BedTransferRequestDto>>(list.OrderByDescending(t => t.RequestedAtUtc).ToList());
    }

    public Task<BedTransferRequestDto> RequestTransferAsync(CreateBedTransferRequest request, string requestedBy, CancellationToken cancellationToken = default)
    {
        var transferId = Guid.NewGuid();
        var transfer = new BedTransferRequestDto
        {
            Id = transferId,
            PatientId = request.PatientId,
            PatientName = request.PatientName,
            PatientMrn = request.PatientMrn,
            FromUnit = request.FromUnit,
            ToUnit = request.ToUnit,
            RequestedBedType = request.RequestedBedType,
            Priority = request.Priority,
            Status = "Requested",
            RequestedAtUtc = DateTimeOffset.UtcNow,
            RequesterName = requestedBy,
            ClinicalNotes = request.ClinicalNotes
        };

        _transfers[transferId] = transfer;
        return Task.FromResult(transfer);
    }

    public Task<BedTransferRequestDto> AssignBedAsync(Guid requestId, Guid bedId, string assignedBy, CancellationToken cancellationToken = default)
    {
        if (!_transfers.TryGetValue(requestId, out var transfer))
            throw new KeyNotFoundException($"Transfer request {requestId} not found.");

        if (!_beds.TryGetValue(bedId, out var bed))
            throw new KeyNotFoundException($"Bed {bedId} not found.");

        lock (_lock)
        {
            if (bed.Status != "Available")
                throw new InvalidOperationException($"Cannot assign Bed {bed.BedNumber} in {bed.WardName}. Current status is '{bed.Status}' (only 'Available' beds can be assigned).");

            bed.Status = "Reserved";
            bed.CurrentPatientId = transfer.PatientId;
            bed.CurrentPatientName = transfer.PatientName;
            bed.CurrentPatientMrn = transfer.PatientMrn;

            transfer.Status = "BedAssigned";
            transfer.AssignedBedId = bed.Id;
            transfer.AssignedBedNumber = $"{bed.WardName} - {bed.RoomNumber} / {bed.BedNumber}";
            transfer.BedAssignedAtUtc = DateTimeOffset.UtcNow;
        }

        return Task.FromResult(transfer);
    }

    public Task<BedTransferRequestDto> UpdateTransferStageAsync(Guid requestId, string stage, string updatedBy, CancellationToken cancellationToken = default)
    {
        if (!_transfers.TryGetValue(requestId, out var transfer))
            throw new KeyNotFoundException($"Transfer request {requestId} not found.");

        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            transfer.Status = stage;

            if (stage == "Dispatched" && !transfer.DispatchedAtUtc.HasValue)
                transfer.DispatchedAtUtc = now;
            else if (stage == "Received" && !transfer.ReceivedAtUtc.HasValue)
                transfer.ReceivedAtUtc = now;
            else if (stage == "Completed")
            {
                if (!transfer.CompletedAtUtc.HasValue)
                    transfer.CompletedAtUtc = now;

                if (transfer.AssignedBedId.HasValue && _beds.TryGetValue(transfer.AssignedBedId.Value, out var bed))
                {
                    bed.Status = "Occupied";
                    bed.OccupiedSinceUtc = now;
                }
            }
        }

        return Task.FromResult(transfer);
    }

    public Task<BedDto> UpdateBedStatusAsync(Guid bedId, string newStatus, CancellationToken cancellationToken = default)
    {
        if (!_beds.TryGetValue(bedId, out var bed))
            throw new KeyNotFoundException($"Bed {bedId} not found.");

        lock (_lock)
        {
            bed.Status = newStatus;
            if (newStatus == "Available")
            {
                bed.CurrentPatientId = null;
                bed.CurrentPatientName = null;
                bed.CurrentPatientMrn = null;
                bed.LastCleanedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        return Task.FromResult(bed);
    }

    public Task<IReadOnlyList<BedTurnaroundReportDto>> GetTurnaroundReportAsync(CancellationToken cancellationToken = default)
    {
        var reports = new List<BedTurnaroundReportDto>
        {
            new BedTurnaroundReportDto { Unit = "OT to ICU", TotalTransfers = 14, AverageTurnaroundMinutes = 34.5, MedianTurnaroundMinutes = 28.0, P90TurnaroundMinutes = 52.0 },
            new BedTurnaroundReportDto { Unit = "Emergency to General Ward", TotalTransfers = 28, AverageTurnaroundMinutes = 48.2, MedianTurnaroundMinutes = 42.0, P90TurnaroundMinutes = 75.0 },
            new BedTurnaroundReportDto { Unit = "Surgical to Step-down", TotalTransfers = 9, AverageTurnaroundMinutes = 25.1, MedianTurnaroundMinutes = 22.0, P90TurnaroundMinutes = 38.0 }
        };

        return Task.FromResult<IReadOnlyList<BedTurnaroundReportDto>>(reports);
    }
}
