namespace SmartCare.Application.DTOs;

public class PackageDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int UsedSessions { get; set; }
    public int RemainingSessions => TotalSessions - UsedSessions;
    public decimal TotalAmount { get; set; }
    public decimal PricePerSession => TotalSessions > 0 ? Math.Round(TotalAmount / TotalSessions, 2) : 0m;
    public decimal EarnedRevenue => TotalSessions > 0 ? Math.Round((TotalAmount / TotalSessions) * UsedSessions, 2) : 0m;
    public decimal DeferredRevenue => TotalAmount - EarnedRevenue;
    public string Status => UsedSessions >= TotalSessions ? "Completed" : "Active";
    public DateTime PurchasedAt { get; set; }
}
