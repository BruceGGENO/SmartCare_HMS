namespace SmartCare.Application.DTOs.Requests;

public sealed class PurchasePackageRequest
{
    public int PatientId { get; set; }

    public string PackageName { get; set; } = string.Empty;

    public int TotalSessions { get; set; }

    public decimal TotalAmount { get; set; }
}
