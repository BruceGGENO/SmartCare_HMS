using Microsoft.EntityFrameworkCore;
using SmartCare.Domain.Entities;

namespace SmartCare.Application.Common;

/// <summary>A rule of the hospital was broken (not a bug). The message is safe to show to the user.</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}

public class FileStorageOptions
{
    public string RootPath { get; set; } = "App_Data/uploads";
    public long MaxBytes { get; set; } = 10 * 1024 * 1024;
}

/// <summary>Lookups shared by several seats.</summary>
public class ClinicalLookup
{
    private readonly IAppDbContext _db;
    public ClinicalLookup(IAppDbContext db) => _db = db;

    /// <summary>Visits that are still open: used when a doctor prescribes or orders a test.</summary>
    public Task<List<Encounter>> GetOpenEncountersAsync(CancellationToken ct = default) =>
        _db.Set<Encounter>().AsNoTracking().Include(e => e.Patient)
           .Where(e => e.EndedAtUtc == null)
           .OrderByDescending(e => e.StartedAtUtc).Take(200).ToListAsync(ct);

    public Task<List<PriceItem>> GetPriceItemsAsync(CancellationToken ct = default) =>
        _db.Set<PriceItem>().AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);

    /// <summary>Current price of one unit (0 if the item has no price yet; Seat E sees that in the event).</summary>
    public static async Task<decimal> CurrentPriceAsync(IAppDbContext db, Guid? priceItemId, CancellationToken ct = default)
    {
        if (priceItemId is null) return 0m;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var price = await db.Set<PriceItem>()
            .Where(p => p.Id == priceItemId && p.EffectiveFrom <= today && (p.EffectiveTo == null || p.EffectiveTo >= today))
            .Select(p => (decimal?)p.UnitPrice).FirstOrDefaultAsync(ct);
        return price ?? 0m;
    }
}
