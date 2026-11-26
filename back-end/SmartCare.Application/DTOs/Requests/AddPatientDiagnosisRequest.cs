namespace SmartCare.Application.DTOs.Requests;

public sealed class AddPatientDiagnosisRequest
{
    public string IcdCode { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
