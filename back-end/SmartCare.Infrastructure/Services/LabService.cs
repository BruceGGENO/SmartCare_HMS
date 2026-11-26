using System.Collections.Concurrent;
using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;

namespace SmartCare.Infrastructure.Services;

public sealed class LabService : ILabService
{
    private readonly ISqlExecutor _sql;
    private static readonly List<LabCatalogItemDto> _catalog =
    [
        new LabCatalogItemDto { Id = Guid.Parse("22222222-0000-0000-0000-000000000001"), Code = "LAB-CBC", Name = "Complete Blood Count (CBC) with Differential", Category = "Hematology", UnitPrice = 5000m, ReferenceRange = "WBC: 4.5-11.0 10^3/uL, RBC: 4.3-5.9 10^6/uL, Hgb: 13.5-17.5 g/dL", SpecimenType = "Whole Blood (EDTA)", TurnaroundHours = 2 },
        new LabCatalogItemDto { Id = Guid.Parse("22222222-0000-0000-0000-000000000002"), Code = "LAB-BMP", Name = "Basic Metabolic Panel (BMP / Electrolytes)", Category = "Biochemistry", UnitPrice = 7500m, ReferenceRange = "Na: 136-145 mmol/L, K: 3.5-5.1 mmol/L, Creatinine: 0.7-1.3 mg/dL", SpecimenType = "Serum (SST)", TurnaroundHours = 3 },
        new LabCatalogItemDto { Id = Guid.Parse("22222222-0000-0000-0000-000000000003"), Code = "LAB-LIPID", Name = "Lipid Profile Panel (Total, HDL, LDL, Triglycerides)", Category = "Biochemistry", UnitPrice = 8000m, ReferenceRange = "Total Chol: < 200 mg/dL, Triglycerides: < 150 mg/dL", SpecimenType = "Serum Fasting", TurnaroundHours = 4 },
        new LabCatalogItemDto { Id = Guid.Parse("22222222-0000-0000-0000-000000000004"), Code = "RAD-CXR", Name = "Chest Radiography (X-Ray) PA & Lateral Views", Category = "Radiology", UnitPrice = 12000m, ReferenceRange = "Clear bilateral lung fields, normal cardiothoracic ratio", SpecimenType = "Imaging", TurnaroundHours = 2 },
        new LabCatalogItemDto { Id = Guid.Parse("22222222-0000-0000-0000-000000000005"), Code = "LAB-MAL", Name = "Malaria Rapid Diagnostic Test (RDT) + Blood Smear", Category = "Parasitology", UnitPrice = 2500m, ReferenceRange = "Negative for Plasmodium falciparum / vivax", SpecimenType = "Capillary Blood", TurnaroundHours = 1 }
    ];

    private static readonly ConcurrentDictionary<Guid, LabOrderDto> _orders = new();
    private static readonly object _lock = new();

    static LabService()
    {
        var o1Id = Guid.NewGuid();
        _orders[o1Id] = new LabOrderDto
        {
            Id = o1Id,
            OrderNumber = "ORD-2026-0881",
            PatientId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            PatientName = "Demo Patient",
            PatientMrn = "SC-0001",
            TestName = "Complete Blood Count (CBC) with Differential",
            Category = "Hematology",
            Priority = "Urgent",
            SampleStatus = "Completed",
            SampleId = "SMP-88123-EDTA",
            OrderedBy = "Dr. Robert Mugabe",
            OrderedAtUtc = DateTimeOffset.UtcNow.AddHours(-4),
            ResultValue = "WBC: 14.8 (Elevated), RBC: 4.8, Hgb: 14.2 g/dL",
            ReferenceRange = "WBC: 4.5-11.0 10^3/uL",
            Unit = "10^3/uL",
            IsAbnormal = true,
            IsCritical = false,
            IsVerified = true,
            VerifiedBy = "Diane Uwamahoro, MLS",
            VerifiedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            TechnicianNotes = "Leukocytosis noted consistent with bacterial infection. Verified and released."
        };

        var o2Id = Guid.NewGuid();
        _orders[o2Id] = new LabOrderDto
        {
            Id = o2Id,
            OrderNumber = "ORD-2026-0882",
            PatientId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            PatientName = "Jean Uwimana",
            PatientMrn = "SC-0002",
            TestName = "Basic Metabolic Panel (BMP / Electrolytes)",
            Category = "Biochemistry",
            Priority = "Routine",
            SampleStatus = "In-process",
            SampleId = "SMP-88124-SST",
            OrderedBy = "Dr. Robert Mugabe",
            OrderedAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
            ReferenceRange = "Na: 136-145 mmol/L, K: 3.5-5.1 mmol/L, Creatinine: 0.7-1.3 mg/dL",
            IsVerified = false
        };
    }

    public LabService(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public Task<IReadOnlyList<LabCatalogItemDto>> GetCatalogAsync(string? category = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Task.FromResult<IReadOnlyList<LabCatalogItemDto>>(_catalog);

        var filtered = _catalog.Where(c => c.Category.Contains(category, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<LabCatalogItemDto>>(filtered);
    }

    public Task<IReadOnlyList<LabOrderDto>> GetOrdersAsync(string? status = null, Guid? patientId = null, CancellationToken cancellationToken = default)
    {
        var list = _orders.Values.ToList();
        if (!string.IsNullOrWhiteSpace(status))
            list = list.Where(o => string.Equals(o.SampleStatus, status, StringComparison.OrdinalIgnoreCase)).ToList();

        if (patientId.HasValue)
            list = list.Where(o => o.PatientId == patientId.Value).ToList();

        return Task.FromResult<IReadOnlyList<LabOrderDto>>(list.OrderByDescending(o => o.OrderedAtUtc).ToList());
    }

    public Task<LabOrderDto> CreateOrderAsync(CreateLabOrderRequest request, string orderedBy, CancellationToken cancellationToken = default)
    {
        var test = _catalog.FirstOrDefault(c => c.Id == request.TestCatalogItemId)
            ?? _catalog[0];

        var orderId = Guid.NewGuid();
        var sampleId = $"SMP-{Random.Shared.Next(10000, 99999)}-{test.Category[..3].ToUpperInvariant()}";
        var order = new LabOrderDto
        {
            Id = orderId,
            OrderNumber = $"ORD-2026-{_orders.Count + 1001}",
            PatientId = request.PatientId,
            PatientName = request.PatientName,
            PatientMrn = "SC-" + Random.Shared.Next(1000, 9999),
            TestName = test.Name,
            Category = test.Category,
            Priority = request.Priority,
            SampleStatus = "Collected",
            SampleId = sampleId,
            OrderedBy = orderedBy,
            OrderedAtUtc = DateTimeOffset.UtcNow,
            ReferenceRange = test.ReferenceRange,
            IsVerified = false
        };

        _orders[orderId] = order;
        return Task.FromResult(order);
    }

    public Task<LabOrderDto> UpdateSampleStatusAsync(Guid orderId, UpdateSampleStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!_orders.TryGetValue(orderId, out var order))
            throw new KeyNotFoundException($"Lab order {orderId} not found.");

        lock (_lock)
        {
            order.SampleStatus = request.Status;
            if (!string.IsNullOrWhiteSpace(request.SampleBarcode))
                order.SampleId = request.SampleBarcode;
        }

        return Task.FromResult(order);
    }

    public Task<LabOrderDto> SubmitResultAsync(Guid orderId, SubmitLabResultRequest request, string technicianName, CancellationToken cancellationToken = default)
    {
        if (!_orders.TryGetValue(orderId, out var order))
            throw new KeyNotFoundException($"Lab order {orderId} not found.");

        lock (_lock)
        {
            order.ResultValue = request.ResultValue;
            order.Unit = request.Unit;
            order.IsAbnormal = request.IsAbnormal;
            order.IsCritical = request.IsCritical;
            order.TechnicianNotes = request.TechnicianNotes;
            order.SampleStatus = "Completed";
        }

        return Task.FromResult(order);
    }

    public Task<LabOrderDto> VerifyResultAsync(Guid orderId, string verifierName, CancellationToken cancellationToken = default)
    {
        if (!_orders.TryGetValue(orderId, out var order))
            throw new KeyNotFoundException($"Lab order {orderId} not found.");

        lock (_lock)
        {
            order.IsVerified = true;
            order.VerifiedBy = verifierName;
            order.VerifiedAtUtc = DateTimeOffset.UtcNow;
            order.SampleStatus = "Completed";
        }

        return Task.FromResult(order);
    }
}
