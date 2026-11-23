using Microsoft.EntityFrameworkCore;
using SmartCare.Application.Common;
using SmartCare.Application.Contracts;
using SmartCare.Domain.Common;
using SmartCare.Domain.Entities;

namespace SmartCare.Application.Pharmacy;

public record DrugStock(Drug Drug, int Available, int Expired)
{
    public bool IsLow => Available <= Drug.ReorderLevel;
}

public record AllergyConflict(Drug Drug, Allergy Allergy);

/// <summary>One line when goods arrive against a purchase order.</summary>
public class ReceiptLine
{
    public Guid LineId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public int Quantity { get; set; }
}

public class AllergyWarningException : BusinessRuleException
{
    public IReadOnlyList<AllergyConflict> Conflicts { get; }

    public AllergyWarningException(IReadOnlyList<AllergyConflict> conflicts)
        : base("ALLERGY WARNING: " + string.Join("; ", conflicts.Select(c =>
              $"patient is allergic to {c.Allergy.Substance} ({c.Allergy.Severity}) and {c.Drug.Name} conflicts with it"))
              + ". Enter an override reason to continue.")
    {
        Conflicts = conflicts;
    }
}

/// <summary>MOD-05 Pharmacy and inventory (FR-PHM-001 to 008).</summary>
public class PharmacyService
{
    public const string DefaultStore = "Main Pharmacy";
    public const int NearExpiryDays = 90;

    private readonly IAppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IServiceCompletedPublisher _events;

    public PharmacyService(IAppDbContext db, ICurrentUser user, IServiceCompletedPublisher events)
    {
        _db = db;
        _user = user;
        _events = events;
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // ------------------------------------------------------------ catalog (FR-PHM-001)

    public Task<List<Drug>> GetDrugsAsync(CancellationToken ct = default) =>
        _db.Set<Drug>().AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync(ct);

    public Task<List<Supplier>> GetSuppliersAsync(CancellationToken ct = default) =>
        _db.Set<Supplier>().AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);

    public async Task<Drug> CreateDrugAsync(Drug input, CancellationToken ct = default)
    {
        var code = (input.Code ?? string.Empty).Trim().ToUpperInvariant();
        var name = (input.Name ?? string.Empty).Trim();
        if (code.Length == 0 || name.Length == 0) throw new BusinessRuleException("Drug code and name are required.");
        if (input.ReorderLevel < 0 || input.ReorderQuantity < 0) throw new BusinessRuleException("Reorder level and quantity cannot be negative.");
        if (await _db.Set<Drug>().AnyAsync(d => d.Code == code, ct)) throw new BusinessRuleException($"A drug with code {code} already exists.");

        var drug = new Drug
        {
            Code = code, Name = name,
            GenericName = Clean(input.GenericName), DrugClass = Clean(input.DrugClass),
            Form = Clean(input.Form), Strength = Clean(input.Strength),
            ReorderLevel = input.ReorderLevel, ReorderQuantity = input.ReorderQuantity,
            PriceItemId = input.PriceItemId
        };
        _db.Set<Drug>().Add(drug);
        await _db.SaveChangesAsync(ct);
        return drug;
    }

    public async Task<Supplier> CreateSupplierAsync(string name, string? phone, string? email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleException("Supplier name is required.");
        var supplier = new Supplier { Name = name.Trim(), Phone = Clean(phone), Email = Clean(email) };
        _db.Set<Supplier>().Add(supplier);
        await _db.SaveChangesAsync(ct);
        return supplier;
    }

    // ------------------------------------------------------------ stock (FR-PHM-001, 002, 005, 006)

    public Task<List<StockBatch>> GetBatchesAsync(CancellationToken ct = default) =>
        _db.Set<StockBatch>().AsNoTracking().Include(b => b.Drug)
           .Where(b => b.QuantityOnHand > 0)
           .OrderBy(b => b.Drug.Name).ThenBy(b => b.ExpiryDate).ToListAsync(ct);

    public async Task<List<DrugStock>> GetStockLevelsAsync(CancellationToken ct = default)
    {
        var today = Today;
        var drugs = await GetDrugsAsync(ct);
        var batches = await _db.Set<StockBatch>().AsNoTracking().Where(b => b.QuantityOnHand > 0).ToListAsync(ct);
        return drugs.Select(d => new DrugStock(
            d,
            batches.Where(b => b.DrugId == d.Id && b.ExpiryDate >= today).Sum(b => b.QuantityOnHand),
            batches.Where(b => b.DrugId == d.Id && b.ExpiryDate < today).Sum(b => b.QuantityOnHand))).ToList();
    }

    public Task<List<StockBatch>> GetNearExpiryBatchesAsync(int days = NearExpiryDays, CancellationToken ct = default)
    {
        var today = Today;
        var limit = today.AddDays(days);
        return _db.Set<StockBatch>().AsNoTracking().Include(b => b.Drug)
            .Where(b => b.QuantityOnHand > 0 && b.ExpiryDate >= today && b.ExpiryDate <= limit)
            .OrderBy(b => b.ExpiryDate).ToListAsync(ct);
    }

    public Task<List<StockBatch>> GetExpiredBatchesAsync(CancellationToken ct = default)
    {
        var today = Today;
        return _db.Set<StockBatch>().AsNoTracking().Include(b => b.Drug)
            .Where(b => b.QuantityOnHand > 0 && b.ExpiryDate < today)
            .OrderBy(b => b.ExpiryDate).ToListAsync(ct);
    }

    /// <summary>Adds stock for a delivery. Same drug + store + batch number tops up the existing batch.</summary>
    public async Task<StockBatch> ReceiveStockAsync(Guid drugId, string store, string batchNumber, DateOnly expiry,
        int quantity, decimal unitCost, Guid? supplierId, CancellationToken ct = default)
    {
        var batch = await ApplyReceiptAsync(drugId, store, batchNumber, expiry, quantity, unitCost, supplierId, "Stock received", ct);
        await SaveAsync(ct);
        return batch;
    }

    private async Task<StockBatch> ApplyReceiptAsync(Guid drugId, string? store, string batchNumber, DateOnly expiry,
        int quantity, decimal unitCost, Guid? supplierId, string reason, CancellationToken ct)
    {
        if (quantity <= 0) throw new BusinessRuleException("Quantity received must be more than zero.");
        if (string.IsNullOrWhiteSpace(batchNumber)) throw new BusinessRuleException("Batch number is required.");
        if (expiry <= Today) throw new BusinessRuleException("This batch is already expired; it cannot be received into stock.");
        if (unitCost < 0) throw new BusinessRuleException("Unit cost cannot be negative.");

        var drug = await _db.Set<Drug>().FirstOrDefaultAsync(d => d.Id == drugId, ct)
                   ?? throw new BusinessRuleException("Drug not found.");
        var storeName = Clean(store) ?? DefaultStore;
        var number = batchNumber.Trim();

        var set = _db.Set<StockBatch>();
        var batch = set.Local.FirstOrDefault(b => b.DrugId == drugId && b.StoreName == storeName && b.BatchNumber == number)
                    ?? await set.FirstOrDefaultAsync(b => b.DrugId == drugId && b.StoreName == storeName && b.BatchNumber == number, ct);

        if (batch is null)
        {
            batch = new StockBatch
            {
                DrugId = drugId, StoreName = storeName, BatchNumber = number,
                ExpiryDate = expiry, QuantityOnHand = quantity, UnitCost = unitCost, SupplierId = supplierId
            };
            set.Add(batch);
        }
        else
        {
            if (batch.ExpiryDate != expiry)
                throw new BusinessRuleException($"Batch {number} already exists with expiry {batch.ExpiryDate:yyyy-MM-dd}. Use a different batch number if the expiry differs.");
            batch.QuantityOnHand += quantity;
            batch.UnitCost = unitCost;
        }

        _db.Set<StockMovement>().Add(new StockMovement
        {
            StockBatch = batch, Type = StockMovementType.Receipt, Quantity = quantity,
            Reason = reason, ByUserId = _user.UserId
        });

        // Stock is back above the reorder level: close the alert so a future breach fires again
        if (drug.ReorderAlertActive) drug.ReorderAlertActive = false;
        return batch;
    }

    /// <summary>Correct a stock count (stock-take). Needs a reason; stock can never go below zero.</summary>
    public async Task AdjustStockAsync(Guid batchId, int delta, string reason, CancellationToken ct = default)
    {
        if (delta == 0) throw new BusinessRuleException("The adjustment cannot be zero.");
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessRuleException("A reason is required for every stock adjustment.");
        var batch = await _db.Set<StockBatch>().FirstOrDefaultAsync(b => b.Id == batchId, ct)
                    ?? throw new BusinessRuleException("Batch not found.");
        if (batch.QuantityOnHand + delta < 0) throw new BusinessRuleException("This adjustment would make stock negative.");

        batch.QuantityOnHand += delta;
        _db.Set<StockMovement>().Add(new StockMovement
        {
            StockBatchId = batch.Id, Type = StockMovementType.Adjustment, Quantity = delta,
            Reason = reason.Trim(), ByUserId = _user.UserId
        });
        await SaveAsync(ct);
    }

    /// <summary>Remove damaged or expired stock from the shelves.</summary>
    public async Task WriteOffAsync(Guid batchId, int quantity, string reason, CancellationToken ct = default)
    {
        if (quantity <= 0) throw new BusinessRuleException("Write-off quantity must be more than zero.");
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessRuleException("A reason is required for every write-off.");
        var batch = await _db.Set<StockBatch>().FirstOrDefaultAsync(b => b.Id == batchId, ct)
                    ?? throw new BusinessRuleException("Batch not found.");
        if (quantity > batch.QuantityOnHand) throw new BusinessRuleException($"Only {batch.QuantityOnHand} left in this batch.");

        batch.QuantityOnHand -= quantity;
        _db.Set<StockMovement>().Add(new StockMovement
        {
            StockBatchId = batch.Id, Type = StockMovementType.WriteOff, Quantity = -quantity,
            Reason = reason.Trim(), ByUserId = _user.UserId
        });
        await SaveAsync(ct);
    }

    /// <summary>
    /// Raises a reorder alert when available stock falls to the reorder level. The alert fires once per breach:
    /// it is closed again when stock goes back above the level. Returns the alerts raised by this call.
    /// </summary>
    public async Task<List<Drug>> CheckReorderAlertsAsync(CancellationToken ct = default)
    {
        var today = Today;
        var drugs = await _db.Set<Drug>().Where(d => d.IsActive).ToListAsync(ct);
        var available = await _db.Set<StockBatch>()
            .Where(b => b.QuantityOnHand > 0 && b.ExpiryDate >= today)
            .GroupBy(b => b.DrugId)
            .Select(g => new { DrugId = g.Key, Qty = g.Sum(x => x.QuantityOnHand) })
            .ToListAsync(ct);

        var raised = new List<Drug>();
        foreach (var d in drugs)
        {
            var qty = available.FirstOrDefault(a => a.DrugId == d.Id)?.Qty ?? 0;
            if (qty <= d.ReorderLevel && !d.ReorderAlertActive) { d.ReorderAlertActive = true; raised.Add(d); }
            else if (qty > d.ReorderLevel && d.ReorderAlertActive) d.ReorderAlertActive = false;
        }
        await _db.SaveChangesAsync(ct);
        return raised;
    }

    // ------------------------------------------------------------ allergies (FR-EHR-003, FR-PHM-004)

    /// <summary>
    /// Compares the patient's allergies with the drug name, generic name and drug class.
    /// This is a name check only; a full drug-interaction database is not part of this release.
    /// </summary>
    public async Task<List<AllergyConflict>> FindAllergyConflictsAsync(Guid patientId, IEnumerable<Guid> drugIds, CancellationToken ct = default)
    {
        var ids = drugIds.ToList();
        var allergies = await _db.Set<Allergy>().AsNoTracking().Where(a => a.PatientId == patientId).ToListAsync(ct);
        if (allergies.Count == 0) return new List<AllergyConflict>();
        var drugs = await _db.Set<Drug>().AsNoTracking().Where(d => ids.Contains(d.Id)).ToListAsync(ct);

        var conflicts = new List<AllergyConflict>();
        foreach (var drug in drugs)
            foreach (var allergy in allergies)
                if (Matches(allergy.Substance, drug.Name) || Matches(allergy.Substance, drug.GenericName) || Matches(allergy.Substance, drug.DrugClass))
                    conflicts.Add(new AllergyConflict(drug, allergy));
        return conflicts;
    }

    private static bool Matches(string substance, string? drugText)
    {
        if (string.IsNullOrWhiteSpace(substance) || string.IsNullOrWhiteSpace(drugText)) return false;
        var a = substance.Trim();
        var d = drugText.Trim();
        if (a.Length < 3 || d.Length < 3) return false;
        return d.Contains(a, StringComparison.OrdinalIgnoreCase) || a.Contains(d, StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------ prescriptions

    /// <summary>A doctor prescribes. Warns (blocks) on an allergy conflict unless an override reason is given.</summary>
    public async Task<Prescription> CreatePrescriptionAsync(Guid encounterId, List<PrescriptionItem> items,
        string? allergyOverrideReason, CancellationToken ct = default)
    {
        var doctorId = _user.StaffId ?? throw new BusinessRuleException("Your account is not linked to a staff profile, so you cannot prescribe.");
        items = items.Where(i => i.DrugId != Guid.Empty).ToList();
        if (items.Count == 0) throw new BusinessRuleException("Add at least one drug.");

        var encounter = await _db.Set<Encounter>().FirstOrDefaultAsync(e => e.Id == encounterId, ct)
                        ?? throw new BusinessRuleException("Visit (encounter) not found.");
        if (encounter.EndedAtUtc is not null) throw new BusinessRuleException("This visit is already closed.");

        foreach (var i in items)
        {
            if (i.QuantityPrescribed <= 0) throw new BusinessRuleException("Quantity must be more than zero for every drug.");
            if (string.IsNullOrWhiteSpace(i.Dose) || string.IsNullOrWhiteSpace(i.Frequency))
                throw new BusinessRuleException("Dose and frequency are required for every drug.");
        }

        var conflicts = await FindAllergyConflictsAsync(encounter.PatientId, items.Select(i => i.DrugId), ct);
        if (conflicts.Count > 0 && string.IsNullOrWhiteSpace(allergyOverrideReason))
            throw new AllergyWarningException(conflicts);

        var prescription = new Prescription
        {
            PatientId = encounter.PatientId, EncounterId = encounter.Id, DoctorId = doctorId,
            AllergyOverrideReason = conflicts.Count > 0 ? allergyOverrideReason!.Trim() : null
        };
        foreach (var i in items)
            prescription.Items.Add(new PrescriptionItem
            {
                PatientId = encounter.PatientId, DrugId = i.DrugId, Dose = i.Dose.Trim(),
                Frequency = i.Frequency.Trim(), Duration = Clean(i.Duration), QuantityPrescribed = i.QuantityPrescribed
            });

        _db.Set<Prescription>().Add(prescription);
        await _db.SaveChangesAsync(ct);
        return prescription;
    }

    public Task<List<Prescription>> GetPendingPrescriptionsAsync(CancellationToken ct = default) =>
        _db.Set<Prescription>().AsNoTracking()
           .Include(p => p.Patient)
           .Include(p => p.Items).ThenInclude(i => i.Drug)
           .Where(p => p.Status == PrescriptionStatus.Active || p.Status == PrescriptionStatus.PartiallyDispensed)
           .OrderBy(p => p.CreatedAtUtc).ToListAsync(ct);

    public Task<PrescriptionItem?> GetPrescriptionItemAsync(Guid itemId, CancellationToken ct = default) =>
        _db.Set<PrescriptionItem>().AsNoTracking()
           .Include(i => i.Drug)
           .Include(i => i.Prescription).ThenInclude(p => p.Patient).ThenInclude(p => p.Allergies)
           .FirstOrDefaultAsync(i => i.Id == itemId, ct);

    // ------------------------------------------------------------ dispensing (FR-PHM-002, 003, 004, 006, 008)

    /// <summary>
    /// Gives medicine against a prescription line. Uses unexpired batches, earliest expiry first.
    /// Partial dispensing is allowed. Stock can never go negative. The billing event is saved in the same transaction.
    /// </summary>
    public async Task<List<DispenseRecord>> DispenseAsync(Guid prescriptionItemId, int quantity, string? allergyOverrideReason,
        string store = DefaultStore, CancellationToken ct = default)
    {
        var userId = _user.UserId ?? throw new BusinessRuleException("You must be signed in to dispense.");
        if (quantity <= 0) throw new BusinessRuleException("Quantity must be more than zero.");

        var item = await _db.Set<PrescriptionItem>()
            .Include(i => i.Drug)
            .Include(i => i.Prescription)
            .FirstOrDefaultAsync(i => i.Id == prescriptionItemId, ct)
            ?? throw new BusinessRuleException("Prescription item not found.");

        var prescription = item.Prescription;
        if (prescription.Status is PrescriptionStatus.Cancelled or PrescriptionStatus.Dispensed)
            throw new BusinessRuleException($"This prescription is {prescription.Status}; nothing more can be dispensed.");

        var remaining = item.QuantityPrescribed - item.QuantityDispensed;
        if (quantity > remaining) throw new BusinessRuleException($"Only {remaining} of {item.Drug.Name} is still to be dispensed on this prescription.");

        // Allergy check: blocking warning, override needs a reason
        var conflicts = await FindAllergyConflictsAsync(item.PatientId, new[] { item.DrugId }, ct);
        if (conflicts.Count > 0 && string.IsNullOrWhiteSpace(allergyOverrideReason))
            throw new AllergyWarningException(conflicts);

        // Expired batches are never selected; earliest expiry is used first
        var today = Today;
        var storeName = Clean(store) ?? DefaultStore;
        var batches = await _db.Set<StockBatch>()
            .Where(b => b.DrugId == item.DrugId && b.StoreName == storeName && b.QuantityOnHand > 0 && b.ExpiryDate >= today)
            .OrderBy(b => b.ExpiryDate).ThenBy(b => b.CreatedAtUtc)
            .ToListAsync(ct);

        var available = batches.Sum(b => b.QuantityOnHand);
        if (available < quantity)
            throw new BusinessRuleException($"Not enough unexpired stock of {item.Drug.Name}. Available: {available}, requested: {quantity}.");

        var records = new List<DispenseRecord>();
        var left = quantity;
        foreach (var batch in batches)
        {
            if (left == 0) break;
            var take = Math.Min(left, batch.QuantityOnHand);
            batch.QuantityOnHand -= take;
            left -= take;

            var record = new DispenseRecord
            {
                PrescriptionItemId = item.Id, PatientId = item.PatientId, StockBatchId = batch.Id,
                Quantity = take, DispensedByUserId = userId,
                AllergyOverrideReason = conflicts.Count > 0 ? allergyOverrideReason!.Trim() : null
            };
            records.Add(record);
            _db.Set<DispenseRecord>().Add(record);
            _db.Set<StockMovement>().Add(new StockMovement
            {
                StockBatchId = batch.Id, Type = StockMovementType.Dispense, Quantity = -take,
                Reason = "Dispensed against prescription", ByUserId = userId
            });
        }

        item.QuantityDispensed += quantity;

        // Prescription status: partial until every line is complete
        var lines = await _db.Set<PrescriptionItem>().Where(i => i.PrescriptionId == prescription.Id).ToListAsync(ct);
        prescription.Status = lines.All(l => l.QuantityDispensed >= l.QuantityPrescribed)
            ? PrescriptionStatus.Dispensed : PrescriptionStatus.PartiallyDispensed;

        // Reorder alert (once per breach)
        var otherStores = await _db.Set<StockBatch>()
            .Where(b => b.DrugId == item.DrugId && b.StoreName != storeName && b.ExpiryDate >= today)
            .SumAsync(b => (int?)b.QuantityOnHand, ct) ?? 0;
        var totalAfter = available - quantity + otherStores;
        if (totalAfter <= item.Drug.ReorderLevel && !item.Drug.ReorderAlertActive) item.Drug.ReorderAlertActive = true;

        // FR-PHM-008: dispensed items reach billing. Saved together with the stock change.
        var unit = await ClinicalLookup.CurrentPriceAsync(_db, item.Drug.PriceItemId, ct);
        _events.Publish(new ServiceCompleted(
            item.PatientId, prescription.EncounterId, item.Drug.PriceItemId, prescription.DoctorId,
            "Pharmacy", $"{item.Drug.Name} {item.Drug.Strength}".Trim(), quantity, unit, DateTime.UtcNow));

        await SaveAsync(ct);
        return records;
    }

    // ------------------------------------------------------------ purchase orders (FR-PHM-007)

    public Task<List<PurchaseOrder>> GetPurchaseOrdersAsync(CancellationToken ct = default) =>
        _db.Set<PurchaseOrder>().AsNoTracking().Include(p => p.Supplier).Include(p => p.Lines)
           .OrderByDescending(p => p.CreatedAtUtc).ToListAsync(ct);

    public Task<PurchaseOrder?> GetPurchaseOrderAsync(Guid id, CancellationToken ct = default) =>
        _db.Set<PurchaseOrder>().AsNoTracking().Include(p => p.Supplier)
           .Include(p => p.Lines).ThenInclude(l => l.Drug)
           .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PurchaseOrder> CreatePurchaseOrderAsync(Guid supplierId, CancellationToken ct = default)
    {
        if (!await _db.Set<Supplier>().AnyAsync(s => s.Id == supplierId, ct)) throw new BusinessRuleException("Supplier not found.");
        var po = new PurchaseOrder { SupplierId = supplierId };
        _db.Set<PurchaseOrder>().Add(po);
        await _db.SaveChangesAsync(ct);
        return po;
    }

    public async Task AddPurchaseOrderLineAsync(Guid purchaseOrderId, Guid drugId, int quantity, decimal unitCost, CancellationToken ct = default)
    {
        var po = await _db.Set<PurchaseOrder>().FirstOrDefaultAsync(p => p.Id == purchaseOrderId, ct)
                 ?? throw new BusinessRuleException("Purchase order not found.");
        if (po.Status != PurchaseOrderStatus.Draft) throw new BusinessRuleException("Lines can only be added while the order is a Draft.");
        if (quantity <= 0 || unitCost < 0) throw new BusinessRuleException("Quantity must be more than zero and cost cannot be negative.");
        if (!await _db.Set<Drug>().AnyAsync(d => d.Id == drugId, ct)) throw new BusinessRuleException("Drug not found.");

        _db.Set<PurchaseOrderLine>().Add(new PurchaseOrderLine { PurchaseOrderId = po.Id, DrugId = drugId, Quantity = quantity, UnitCost = unitCost });
        await _db.SaveChangesAsync(ct);
    }

    public async Task SendPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken ct = default)
    {
        var po = await _db.Set<PurchaseOrder>().Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == purchaseOrderId, ct)
                 ?? throw new BusinessRuleException("Purchase order not found.");
        if (po.Status != PurchaseOrderStatus.Draft) throw new BusinessRuleException("Only a Draft order can be sent.");
        if (po.Lines.Count == 0) throw new BusinessRuleException("Add at least one line before sending.");
        po.Status = PurchaseOrderStatus.Sent;
        po.OrderedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Goods receipt: updates stock. The order becomes Received when every line is fully received.</summary>
    public async Task ReceivePurchaseOrderAsync(Guid purchaseOrderId, List<ReceiptLine> receipts, CancellationToken ct = default)
    {
        var po = await _db.Set<PurchaseOrder>().Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == purchaseOrderId, ct)
                 ?? throw new BusinessRuleException("Purchase order not found.");
        if (po.Status != PurchaseOrderStatus.Sent) throw new BusinessRuleException("Only a Sent order can be received.");

        var any = false;
        foreach (var r in receipts.Where(r => r.Quantity > 0))
        {
            var line = po.Lines.FirstOrDefault(l => l.Id == r.LineId) ?? throw new BusinessRuleException("Receipt line does not belong to this order.");
            if (r.Quantity > line.Quantity - line.QuantityReceived)
                throw new BusinessRuleException($"Cannot receive {r.Quantity}: only {line.Quantity - line.QuantityReceived} still expected on this line.");
            await ApplyReceiptAsync(line.DrugId, DefaultStore, r.BatchNumber, r.ExpiryDate, r.Quantity, line.UnitCost, po.SupplierId,
                $"Goods receipt for purchase order {po.Id.ToString()[..8]}", ct);
            line.QuantityReceived += r.Quantity;
            any = true;
        }
        if (!any) throw new BusinessRuleException("Enter a quantity for at least one line.");

        if (po.Lines.All(l => l.QuantityReceived >= l.Quantity))
        {
            po.Status = PurchaseOrderStatus.Received;
            po.ReceivedAtUtc = DateTime.UtcNow;
        }
        await SaveAsync(ct);
    }

    // ------------------------------------------------------------ helpers

    /// <summary>Two people changing the same stock at the same time: one wins, the other is asked to retry.</summary>
    private async Task SaveAsync(CancellationToken ct)
    {
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessRuleException("The stock or prescription was changed by someone else at the same moment. Nothing was saved; please check the screen and try again.");
        }
    }
}

/// <summary>Seat C asks this when the doctor finalizes a discharge: can Pharmacy clear the patient?</summary>
public class PharmacyClearanceCheck : IDepartmentClearanceCheck
{
    private readonly IAppDbContext _db;
    public PharmacyClearanceCheck(IAppDbContext db) => _db = db;
    public string DepartmentName => "Pharmacy";

    public async Task<ClearanceCheckResult> CheckAsync(Guid encounterId, CancellationToken ct = default)
    {
        var open = await _db.Set<PrescriptionItem>().CountAsync(i =>
            i.Prescription.EncounterId == encounterId
            && (i.Prescription.Status == PrescriptionStatus.Active || i.Prescription.Status == PrescriptionStatus.PartiallyDispensed)
            && i.QuantityDispensed < i.QuantityPrescribed, ct);
        return open == 0
            ? new ClearanceCheckResult(true, null)
            : new ClearanceCheckResult(false, $"{open} prescribed item(s) not yet dispensed.");
    }
}
