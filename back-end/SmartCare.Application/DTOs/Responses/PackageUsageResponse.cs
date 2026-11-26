namespace SmartCare.Application.DTOs.Responses;

public sealed class PackageUsageResponse
{
    public int UsageId { get; set; }

    public int PackageId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string PackageName { get; set; } = string.Empty;

    public int SessionNumber { get; set; }

    public decimal RecognizedAmount { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset ConsumedAtUtc { get; set; }
}
