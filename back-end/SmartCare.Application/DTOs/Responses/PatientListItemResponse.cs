namespace SmartCare.Application.DTOs.Responses;

public sealed class PatientListItemResponse
{
    public int PatientId { get; set; }

    public string Mrn { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}
