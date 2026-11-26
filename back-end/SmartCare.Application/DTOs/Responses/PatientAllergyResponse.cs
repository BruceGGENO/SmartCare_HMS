namespace SmartCare.Application.DTOs.Responses;

public sealed class PatientAllergyResponse
{
    public int AllergyId { get; set; }

    public int PatientId { get; set; }

    public string Substance { get; set; } = string.Empty;

    public string? Reaction { get; set; }

    public string? Severity { get; set; }

    public DateTimeOffset NotedAtUtc { get; set; }
}
