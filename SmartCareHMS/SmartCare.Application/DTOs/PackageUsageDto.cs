namespace SmartCare.Application.DTOs;

public class PackageUsageDto
{
    public int Id { get; set; }
    public int PackageId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public int SessionNumber { get; set; }
    public decimal RecognizedAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime ConsumedAt { get; set; }
}
