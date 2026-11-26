using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Services;

public sealed class RevenueService : IRevenueService
{
    private readonly ISqlExecutor _sql;

    public RevenueService(ISqlExecutor sql) => _sql = sql;

    public async Task EnsureDemoDataAsync(CancellationToken cancellationToken = default)
    {
        var existing = await ListPackagesAsync(null, cancellationToken);
        if (existing.Count > 0)
            return;

        var patients = await _sql.QueryAsync(
            "dbo.sp_ListPatients",
            r => r.Get<int>("PatientId"),
            cancellationToken: cancellationToken);

        if (patients.Count == 0)
            return;

        var demos = new (string Name, int Sessions, decimal Amount)[]
        {
            ("Physiotherapy Rehabilitation (5 Sessions)", 5, 250m),
            ("Dialysis Treatment Plan (10 Sessions)", 10, 800m),
            ("Antenatal Wellness Care (4 Sessions)", 4, 200m)
        };

        for (var i = 0; i < demos.Length; i++)
        {
            var patientId = patients[i % patients.Count];
            var (name, sessions, amount) = demos[i];
            var package = await PurchaseAsync(patientId, name, sessions, amount, cancellationToken);

            var consumeCount = i == 0 ? 2 : i == 1 ? 3 : 1;
            for (var s = 0; s < consumeCount; s++)
                await ConsumeSessionAsync(package.PackageId, $"Demo session {s + 1}", cancellationToken);
        }
    }

    public async Task<RevenueSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_GetRevenueSummary",
            r => new RevenueSummaryResponse
            {
                TotalCashCollected = r.Get<decimal>("TotalCashCollected"),
                TotalEarnedRevenue = r.Get<decimal>("TotalEarnedRevenue"),
                TotalDeferredRevenue = r.Get<decimal>("TotalDeferredRevenue"),
                TotalPackagesCount = r.Get<int>("TotalPackagesCount"),
                ActivePackagesCount = r.GetOrNull<int>("ActivePackagesCount") ?? 0,
                TotalSessionsSold = r.Get<int>("TotalSessionsSold"),
                TotalSessionsConsumed = r.Get<int>("TotalSessionsConsumed")
            },
            cancellationToken: cancellationToken);

        return rows.Count == 0 ? new RevenueSummaryResponse() : rows[0];
    }

    public Task<IReadOnlyList<PrepaidPackageResponse>> ListPackagesAsync(
        int? patientId,
        CancellationToken cancellationToken = default) =>
        _sql.QueryAsync(
            "dbo.sp_ListPrepaidPackages",
            MapPackage,
            new Dictionary<string, object?> { ["PatientId"] = patientId },
            cancellationToken);

    public async Task<PrepaidPackageResponse?> GetPackageByIdAsync(
        int packageId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_GetPrepaidPackageById",
            MapPackage,
            new Dictionary<string, object?> { ["PackageId"] = packageId },
            cancellationToken);
        return rows.Count == 0 ? null : rows[0];
    }

    public Task<IReadOnlyList<PackageUsageResponse>> ListRecentUsagesAsync(
        CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_ListRecentPackageUsages", MapUsage, cancellationToken: cancellationToken);

    public async Task<PrepaidPackageResponse> PurchaseAsync(
        int patientId,
        string packageName,
        int totalSessions,
        decimal totalAmount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync(
                "dbo.sp_PurchasePrepaidPackage",
                MapPackage,
                new Dictionary<string, object?>
                {
                    ["PatientId"] = patientId,
                    ["PackageName"] = packageName.Trim(),
                    ["TotalSessions"] = totalSessions,
                    ["TotalAmount"] = totalAmount
                },
                cancellationToken);

            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 50011 or 50012 or 50013)
        {
            throw new ArgumentException(ex.Message.Trim());
        }
    }

    public async Task<PackageUsageResponse> ConsumeSessionAsync(
        int packageId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync(
                "dbo.sp_ConsumePrepaidPackageSession",
                MapUsage,
                new Dictionary<string, object?>
                {
                    ["PackageId"] = packageId,
                    ["Notes"] = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
                },
                cancellationToken);

            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 50014 or 50015)
        {
            throw new InvalidOperationException(ex.Message.Trim());
        }
    }

    public async Task<byte[]> ExportReconciliationCsvAsync(CancellationToken cancellationToken = default)
    {
        var packages = await ListPackagesAsync(null, cancellationToken);
        var summary = await GetSummaryAsync(cancellationToken);
        var sb = new StringBuilder();

        sb.AppendLine(
            "PackageId,PatientName,PatientMrn,PackageName,TotalSessions,UsedSessions,RemainingSessions,PricePerSession,TotalCollected,EarnedRevenue,DeferredRevenue,Status,PurchasedAtUtc");

        foreach (var p in packages)
        {
            sb.AppendLine(string.Join(',',
                p.PackageId,
                Quote(p.PatientName),
                Quote(p.PatientMrn),
                Quote(p.PackageName),
                p.TotalSessions,
                p.UsedSessions,
                p.RemainingSessions,
                p.PricePerSession.ToString("F2"),
                p.TotalAmount.ToString("F2"),
                p.EarnedRevenue.ToString("F2"),
                p.DeferredRevenue.ToString("F2"),
                Quote(p.Status),
                Quote(p.PurchasedAtUtc.ToString("u"))));
        }

        sb.AppendLine();
        sb.AppendLine("RECONCILIATION SUMMARY");
        sb.AppendLine($"TotalCashCollected,{summary.TotalCashCollected:F2}");
        sb.AppendLine($"TotalEarnedRevenue,{summary.TotalEarnedRevenue:F2}");
        sb.AppendLine($"TotalDeferredRevenue,{summary.TotalDeferredRevenue:F2}");
        sb.AppendLine($"TotalSessionsSold,{summary.TotalSessionsSold}");
        sb.AppendLine($"TotalSessionsConsumed,{summary.TotalSessionsConsumed}");
        sb.AppendLine($"RemainingSessions,{summary.RemainingSessions}");
        sb.AppendLine($"EarnedPercentage,{summary.EarnedPercentage}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static PrepaidPackageResponse MapPackage(DbDataReader r) => new()
    {
        PackageId = r.Get<int>("PackageId"),
        PatientId = r.Get<int>("PatientId"),
        PatientName = r.Get<string>("PatientName"),
        PatientMrn = r.Get<string>("PatientMrn"),
        PackageName = r.Get<string>("PackageName"),
        TotalSessions = r.Get<int>("TotalSessions"),
        UsedSessions = r.Get<int>("UsedSessions"),
        TotalAmount = r.Get<decimal>("TotalAmount"),
        PurchasedAtUtc = ReadTime(r, "PurchasedAtUtc")
    };

    private static PackageUsageResponse MapUsage(DbDataReader r) => new()
    {
        UsageId = r.Get<int>("UsageId"),
        PackageId = r.Get<int>("PackageId"),
        PatientName = r.Get<string>("PatientName"),
        PackageName = r.Get<string>("PackageName"),
        SessionNumber = r.Get<int>("SessionNumber"),
        RecognizedAmount = r.Get<decimal>("RecognizedAmount"),
        Notes = r.GetStringOrNull("Notes"),
        ConsumedAtUtc = ReadTime(r, "ConsumedAtUtc")
    };

    private static DateTimeOffset ReadTime(DbDataReader record, string column)
    {
        var value = record.GetValue(record.GetOrdinal(column));
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => DateTimeOffset.UtcNow
        };
    }

    private static string Quote(string value) =>
        "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
}
