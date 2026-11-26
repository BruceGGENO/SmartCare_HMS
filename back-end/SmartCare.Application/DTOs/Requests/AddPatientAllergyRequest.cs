namespace SmartCare.Application.DTOs.Requests;

public sealed class AddPatientAllergyRequest
{
    public string Substance { get; set; } = string.Empty;

    public string? Reaction { get; set; }

    public string? Severity { get; set; }
}
