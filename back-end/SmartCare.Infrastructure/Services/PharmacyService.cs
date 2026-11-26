using System.Collections.Concurrent;
using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;

namespace SmartCare.Infrastructure.Services;

public sealed class PharmacyService : IPharmacyService
{
    private readonly ISqlExecutor _sql;
    private static readonly ConcurrentDictionary<Guid, DrugCatalogDto> _catalog = new();
    private static readonly List<DispenseResultDto> _dispenseHistory = new();
    private static readonly object _lock = new();

    static PharmacyService()
    {
        var d1Id = Guid.Parse("11111111-0000-0000-0000-000000000001");
        _catalog[d1Id] = new DrugCatalogDto
        {
            Id = d1Id,
            BrandName = "Amoxil",
            GenericName = "Amoxicillin Trihydrate",
            Category = "Antibiotic",
            DosageForm = "Capsule",
            Strength = "500 mg",
            UnitPrice = 500m,
            MinimumStockThreshold = 100,
            TotalStock = 350,
            Batches =
            [
                new StockBatchDto { BatchNumber = "AMX-2026-08", DrugId = d1Id, DrugName = "Amoxicillin 500mg", Quantity = 200, ExpiryDate = DateTime.UtcNow.AddMonths(14), Supplier = "Rwanda Biomedical Center (RBC)" },
                new StockBatchDto { BatchNumber = "AMX-2025-11", DrugId = d1Id, DrugName = "Amoxicillin 500mg", Quantity = 150, ExpiryDate = DateTime.UtcNow.AddMonths(4), Supplier = "Rwanda Biomedical Center (RBC)" }
            ]
        };

        var d2Id = Guid.Parse("11111111-0000-0000-0000-000000000002");
        _catalog[d2Id] = new DrugCatalogDto
        {
            Id = d2Id,
            BrandName = "Panadol",
            GenericName = "Paracetamol (Acetaminophen)",
            Category = "Analgesic / Antipyretic",
            DosageForm = "Tablet",
            Strength = "500 mg",
            UnitPrice = 100m,
            MinimumStockThreshold = 250,
            TotalStock = 1200,
            Batches =
            [
                new StockBatchDto { BatchNumber = "PAR-2026-01", DrugId = d2Id, DrugName = "Paracetamol 500mg", Quantity = 1200, ExpiryDate = DateTime.UtcNow.AddYears(2), Supplier = "Pharmakina DRC / East Africa" }
            ]
        };

        var d3Id = Guid.Parse("11111111-0000-0000-0000-000000000003");
        _catalog[d3Id] = new DrugCatalogDto
        {
            Id = d3Id,
            BrandName = "Glucophage",
            GenericName = "Metformin Hydrochloride",
            Category = "Antidiabetic",
            DosageForm = "Tablet",
            Strength = "500 mg",
            UnitPrice = 350m,
            MinimumStockThreshold = 150,
            TotalStock = 85, // Low stock!
            Batches =
            [
                new StockBatchDto { BatchNumber = "MET-2025-09", DrugId = d3Id, DrugName = "Metformin 500mg", Quantity = 85, ExpiryDate = DateTime.UtcNow.AddMonths(8), Supplier = "Merck East Africa" }
            ]
        };

        var d4Id = Guid.Parse("11111111-0000-0000-0000-000000000004");
        _catalog[d4Id] = new DrugCatalogDto
        {
            Id = d4Id,
            BrandName = "Norvasc",
            GenericName = "Amlodipine Besylate",
            Category = "Antihypertensive",
            DosageForm = "Tablet",
            Strength = "5 mg",
            UnitPrice = 450m,
            MinimumStockThreshold = 80,
            TotalStock = 45, // Low stock!
            Batches =
            [
                new StockBatchDto { BatchNumber = "AML-2025-05", DrugId = d4Id, DrugName = "Amlodipine 5mg", Quantity = 45, ExpiryDate = DateTime.UtcNow.AddDays(40), Supplier = "Pfizer Hub Kigali" }
            ]
        };

        var d5Id = Guid.Parse("11111111-0000-0000-0000-000000000005");
        _catalog[d5Id] = new DrugCatalogDto
        {
            Id = d5Id,
            BrandName = "Ceftriaxone",
            GenericName = "Ceftriaxone Sodium",
            Category = "Cephalosporin Antibiotic",
            DosageForm = "Injection",
            Strength = "1 g Vial",
            UnitPrice = 2500m,
            MinimumStockThreshold = 50,
            TotalStock = 110,
            Batches =
            [
                new StockBatchDto { BatchNumber = "CEF-2026-10", DrugId = d5Id, DrugName = "Ceftriaxone 1g", Quantity = 110, ExpiryDate = DateTime.UtcNow.AddMonths(18), Supplier = "RBC Central Medical Store" }
            ]
        };
    }

    public PharmacyService(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public Task<IReadOnlyList<DrugCatalogDto>> GetDrugCatalogAsync(string? query = null, CancellationToken cancellationToken = default)
    {
        var list = _catalog.Values.ToList();
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult<IReadOnlyList<DrugCatalogDto>>(list);

        var q = query.Trim().ToLowerInvariant();
        var filtered = list.Where(d => 
            d.BrandName.ToLowerInvariant().Contains(q) || 
            d.GenericName.ToLowerInvariant().Contains(q) ||
            d.Category.ToLowerInvariant().Contains(q)).ToList();

        return Task.FromResult<IReadOnlyList<DrugCatalogDto>>(filtered);
    }

    public Task<IReadOnlyList<StockBatchDto>> GetStockBatchesAsync(Guid? drugId = null, CancellationToken cancellationToken = default)
    {
        var allBatches = _catalog.Values.SelectMany(d => d.Batches).ToList();
        if (drugId.HasValue)
            allBatches = allBatches.Where(b => b.DrugId == drugId.Value).ToList();

        return Task.FromResult<IReadOnlyList<StockBatchDto>>(allBatches);
    }

    public Task<IReadOnlyList<ReorderAlertDto>> GetReorderAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = _catalog.Values
            .Where(d => d.IsLowStock)
            .Select(d => new ReorderAlertDto
            {
                DrugId = d.Id,
                DrugName = $"{d.BrandName} ({d.GenericName} {d.Strength})",
                CurrentStock = d.TotalStock,
                MinimumThreshold = d.MinimumStockThreshold,
                SuggestedReorderQty = d.MinimumStockThreshold * 3 - d.TotalStock,
                Urgency = d.TotalStock < (d.MinimumStockThreshold / 2) ? "Critical" : "Medium"
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ReorderAlertDto>>(alerts);
    }

    public async Task<DispenseResultDto> DispenseMedicationAsync(
        DispenseMedicationRequest request,
        string dispensedBy,
        CancellationToken cancellationToken = default)
    {
        if (!_catalog.TryGetValue(request.DrugId, out var drug))
            throw new KeyNotFoundException($"Drug with ID {request.DrugId} not found.");

        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity to dispense must be greater than zero.");

        lock (_lock)
        {
            if (drug.TotalStock < request.Quantity)
                throw new InvalidOperationException($"Insufficient stock for {drug.BrandName}. Available: {drug.TotalStock}, Requested: {request.Quantity}. Negative stock blocked.");

            // Deduct from batch FIFO (oldest expiry first)
            var remainingToDeduct = request.Quantity;
            string usedBatch = "Unknown";
            foreach (var batch in drug.Batches.OrderBy(b => b.ExpiryDate))
            {
                if (batch.Quantity >= remainingToDeduct)
                {
                    batch.Quantity -= remainingToDeduct;
                    usedBatch = batch.BatchNumber;
                    remainingToDeduct = 0;
                    break;
                }
                else
                {
                    remainingToDeduct -= batch.Quantity;
                    usedBatch = batch.BatchNumber;
                    batch.Quantity = 0;
                }
            }

            drug.TotalStock -= request.Quantity;

            var billedAmount = drug.UnitPrice * request.Quantity;
            var result = new DispenseResultDto
            {
                Id = Guid.NewGuid(),
                DispenseNumber = $"DSP-{DateTime.UtcNow:yyyyMMdd}-{_dispenseHistory.Count + 101}",
                DrugName = $"{drug.BrandName} ({drug.GenericName} {drug.Strength})",
                Quantity = request.Quantity,
                BatchNumber = usedBatch,
                DispensedBy = dispensedBy,
                PatientName = string.IsNullOrWhiteSpace(request.PatientName) ? "Outpatient / Ward" : request.PatientName,
                BilledAmount = billedAmount,
                DispensedAtUtc = DateTimeOffset.UtcNow,
                BilledToInvoice = true
            };

            _dispenseHistory.Add(result);

            return result;
        }
    }
}
