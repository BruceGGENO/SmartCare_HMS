namespace SmartCare.Application.DTOs.Responses;

public sealed class PatientDiagnosisResponse
{
    public int DiagnosisId { get; set; }

    public int PatientId { get; set; }

    public string IcdCode { get; set; } = string.Empty;

    public string IcdDescription { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public Guid? DiagnosedByUserId { get; set; }

    public DateTimeOffset DiagnosedAtUtc { get; set; }
}
