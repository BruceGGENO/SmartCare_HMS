using System.Text;
using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Services;

public class RevenueAccountingService : IRevenueAccountingService
{
    private readonly ISqlExecutor _sql;
    private static bool _tablesEnsured = false;
    private static readonly object _lock = new();

    public RevenueAccountingService(ISqlExecutor sql)
    {
        _sql = sql;
    }

    private async Task EnsureTablesExistAsync(CancellationToken cancellationToken)
    {
        if (_tablesEnsured) return;

        try
        {
            // Lightweight check and table creation if script hasn't been executed in SSMS yet
            await _sql.ExecuteNonQueryAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Packages')
                BEGIN
                    CREATE TABLE dbo.Packages (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        PatientId INT NOT NULL,
                        PatientName NVARCHAR(100) NOT NULL,
                        PatientMrn NVARCHAR(50) NOT NULL,
                        PackageName NVARCHAR(100) NOT NULL,
                        TotalSessions INT NOT NULL,
                        UsedSessions INT NOT NULL DEFAULT 0,
                        TotalAmount DECIMAL(18,2) NOT NULL,
                        PurchasedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PackageUsages')
                BEGIN
                    CREATE TABLE dbo.PackageUsages (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        PackageId INT NOT NULL FOREIGN KEY REFERENCES dbo.Packages(Id),
                        SessionNumber INT NOT NULL,
                        RecognizedAmount DECIMAL(18,2) NOT NULL,
                        Notes NVARCHAR(200) NULL,
                        ConsumedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                    );
                END;

                IF NOT EXISTS (SELECT * FROM dbo.Packages)
                BEGIN
                    INSERT INTO dbo.Packages (PatientId, PatientName, PatientMrn, PackageName, TotalSessions, UsedSessions, TotalAmount, PurchasedAt)
                    VALUES
                    (1, 'John Doe', 'MRN-1001', 'Physiotherapy Rehabilitation (5 Sessions)', 5, 2, 250.00, DATEADD(DAY, -10, SYSUTCDATETIME())),
                    (2, 'Jane Smith', 'MRN-1002', 'Dialysis Treatment Plan (10 Sessions)', 10, 6, 800.00, DATEADD(DAY, -20, SYSUTCDATETIME())),
                    (3, 'Robert Johnson', 'MRN-1003', 'Antenatal Wellness Care (4 Sessions)', 4, 4, 200.00, DATEADD(DAY, -30, SYSUTCDATETIME())),
                    (4, 'Emily Davis', 'MRN-1004', 'Post-Op Wound Dressing (6 Sessions)', 6, 1, 180.00, DATEADD(DAY, -5, SYSUTCDATETIME())),
                    (5, 'Michael Wilson', 'MRN-1005', 'Cardiac Rehabilitation Bundle (8 Sessions)', 8, 3, 400.00, DATEADD(DAY, -14, SYSUTCDATETIME()));

                    INSERT INTO dbo.PackageUsages (PackageId, SessionNumber, RecognizedAmount, Notes, ConsumedAt)
                    VALUES
                    (1, 1, 50.00, 'Initial physiotherapy assessment and exercises', DATEADD(DAY, -8, SYSUTCDATETIME())),
                    (1, 2, 50.00, 'Resistance training and ultrasound therapy', DATEADD(DAY, -3, SYSUTCDATETIME())),
                    (2, 1, 80.00, 'Dialysis session 1', DATEADD(DAY, -18, SYSUTCDATETIME())),
                    (2, 2, 80.00, 'Dialysis session 2', DATEADD(DAY, -15, SYSUTCDATETIME())),
                    (2, 3, 80.00, 'Dialysis session 3', DATEADD(DAY, -11, SYSUTCDATETIME())),
                    (2, 4, 80.00, 'Dialysis session 4', DATEADD(DAY, -8, SYSUTCDATETIME())),
                    (2, 5, 80.00, 'Dialysis session 5', DATEADD(DAY, -5, SYSUTCDATETIME())),
                    (2, 6, 80.00, 'Dialysis session 6', DATEADD(DAY, -2, SYSUTCDATETIME())),
                    (3, 1, 50.00, 'Trimester 1 ultrasound and vitals', DATEADD(DAY, -25, SYSUTCDATETIME())),
                    (3, 2, 50.00, 'Routine prenatal checkup', DATEADD(DAY, -18, SYSUTCDATETIME())),
                    (3, 3, 50.00, 'Glucose tolerance and bloodwork', DATEADD(DAY, -10, SYSUTCDATETIME())),
                    (3, 4, 50.00, 'Final third-trimester clearance', DATEADD(DAY, -2, SYSUTCDATETIME())),
                    (4, 1, 30.00, 'Surgical incision cleaning and dressing change', DATEADD(DAY, -3, SYSUTCDATETIME())),
                    (5, 1, 50.00, 'Monitored treadmill exercise session', DATEADD(DAY, -12, SYSUTCDATETIME())),
                    (5, 2, 50.00, 'Cardio endurance and blood pressure profiling', DATEADD(DAY, -8, SYSUTCDATETIME())),
                    (5, 3, 50.00, 'ECG telemetry and cool-down routine', DATEADD(DAY, -1, SYSUTCDATETIME()));
                END;
            ", cancellationToken: cancellationToken);

            lock (_lock)
            {
                _tablesEnsured = true;
            }
        }
        catch
        {
            // Non-blocking fallback
        }
    }

    public async Task<RevenueSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTablesExistAsync(cancellationToken);

        var list = await _sql.QueryAsync(
            "dbo.sp_GetRevenueSummary",
            map: r => new RevenueSummaryDto
            {
                TotalCashCollected = r.Get<decimal>("TotalCashCollected"),
                TotalEarnedRevenue = r.Get<decimal>("TotalEarnedRevenue"),
                TotalDeferredRevenue = r.Get<decimal>("TotalDeferredRevenue"),
                TotalPackagesCount = r.Get<int>("TotalPackagesCount"),
                ActivePackagesCount = r.Get<int>("ActivePackagesCount"),
                TotalSessionsSold = r.Get<int>("TotalSessionsSold"),
                TotalSessionsConsumed = r.Get<int>("TotalSessionsConsumed")
            },
            cancellationToken: cancellationToken);

        return list.Count > 0 ? list[0] : new RevenueSummaryDto();
    }

    public async Task<IReadOnlyList<PackageDto>> GetAllPackagesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTablesExistAsync(cancellationToken);

        return await _sql.QueryAsync(
            "dbo.sp_GetAllPackages",
            map: r => new PackageDto
            {
                Id = r.Get<int>("Id"),
                PatientId = r.Get<int>("PatientId"),
                PatientName = r.Get<string>("PatientName"),
                PatientMrn = r.Get<string>("PatientMrn"),
                PackageName = r.Get<string>("PackageName"),
                TotalSessions = r.Get<int>("TotalSessions"),
                UsedSessions = r.Get<int>("UsedSessions"),
                TotalAmount = r.Get<decimal>("TotalAmount"),
                PurchasedAt = r.Get<DateTime>("PurchasedAt")
            },
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<PackageUsageDto>> GetRecentUsagesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTablesExistAsync(cancellationToken);

        return await _sql.QueryAsync(
            "dbo.sp_GetRecentPackageUsages",
            map: r => new PackageUsageDto
            {
                Id = r.Get<int>("Id"),
                PackageId = r.Get<int>("PackageId"),
                PatientName = r.Get<string>("PatientName"),
                PackageName = r.Get<string>("PackageName"),
                SessionNumber = r.Get<int>("SessionNumber"),
                RecognizedAmount = r.Get<decimal>("RecognizedAmount"),
                Notes = r.Get<string?>("Notes"),
                ConsumedAt = r.Get<DateTime>("ConsumedAt")
            },
            cancellationToken: cancellationToken);
    }

    public async Task<bool> PurchasePackageAsync(PurchasePackageDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureTablesExistAsync(cancellationToken);

        var parameters = new Dictionary<string, object?>
        {
            ["PatientId"] = dto.PatientId,
            ["PatientName"] = dto.PatientName,
            ["PatientMrn"] = dto.PatientMrn,
            ["PackageName"] = dto.PackageName,
            ["TotalSessions"] = dto.TotalSessions,
            ["TotalAmount"] = dto.TotalAmount
        };

        var result = await _sql.QueryAsync(
            "dbo.sp_PurchasePackage",
            parameters: parameters,
            map: r => r.Get<decimal>("NewPackageId"),
            cancellationToken: cancellationToken);

        return result.Count > 0;
    }

    public async Task<bool> ConsumeSessionAsync(int packageId, string? notes = null, CancellationToken cancellationToken = default)
    {
        await EnsureTablesExistAsync(cancellationToken);

        var parameters = new Dictionary<string, object?>
        {
            ["PackageId"] = packageId,
            ["Notes"] = notes
        };

        var result = await _sql.QueryAsync(
            "dbo.sp_ConsumePackageSession",
            parameters: parameters,
            map: r => r.Get<int>("Success"),
            cancellationToken: cancellationToken);

        return result.Count > 0 && result[0] == 1;
    }

    public async Task<byte[]> ExportReconciliationCsvAsync(CancellationToken cancellationToken = default)
    {
        var packages = await GetAllPackagesAsync(cancellationToken);
        var summary = await GetSummaryAsync(cancellationToken);

        var sb = new StringBuilder();

        // CSV Header
        sb.AppendLine("Package ID,Patient Name,Patient MRN,Package Name,Total Sessions,Used Sessions,Remaining Sessions,Price Per Session ($),Total Collected ($),Earned Revenue ($),Deferred Revenue ($),Status,Purchase Date");

        foreach (var p in packages)
        {
            sb.AppendFormat("{0},\"{1}\",\"{2}\",\"{3}\",{4},{5},{6},{7:F2},{8:F2},{9:F2},{10:F2},\"{11}\",\"{12:yyyy-MM-dd HH:mm}\"\n",
                p.Id,
                EscapeCsv(p.PatientName),
                EscapeCsv(p.PatientMrn),
                EscapeCsv(p.PackageName),
                p.TotalSessions,
                p.UsedSessions,
                p.RemainingSessions,
                p.PricePerSession,
                p.TotalAmount,
                p.EarnedRevenue,
                p.DeferredRevenue,
                p.Status,
                p.PurchasedAt);
        }

        sb.AppendLine();
        sb.AppendLine("--- RECONCILIATION SUMMARY ---");
        sb.AppendLine($"Total Cash Collected ($),{summary.TotalCashCollected:F2}");
        sb.AppendLine($"Total Recognized Revenue ($),{summary.TotalEarnedRevenue:F2}");
        sb.AppendLine($"Total Deferred Revenue ($),{summary.TotalDeferredRevenue:F2}");
        sb.AppendLine($"Total Sessions Sold,{summary.TotalSessionsSold}");
        sb.AppendLine($"Total Sessions Consumed,{summary.TotalSessionsConsumed}");
        sb.AppendLine($"Remaining Sessions in Packages,{summary.RemainingSessions}");
        sb.AppendLine($"Revenue Earned Ratio,{summary.EarnedPercentage}%");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\"", "\"\"");
    }
}
