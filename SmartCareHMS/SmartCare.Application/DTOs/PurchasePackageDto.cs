namespace SmartCare.Application.DTOs;

public class PurchasePackageDto
{
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public decimal TotalAmount { get; set; }
}
