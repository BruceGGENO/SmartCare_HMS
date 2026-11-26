namespace SmartCare.Application.DTOs.Responses;

public sealed class WardResponse
{
    public int WardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public sealed class BedResponse
{
    public int BedId { get; set; }
    public int WardId { get; set; }
    public string WardName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class AdmissionResponse
{
    public int AdmissionId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public int BedId { get; set; }
    public string BedCode { get; set; } = string.Empty;
    public string WardName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset AdmittedAtUtc { get; set; }
    public DateTimeOffset? DischargedAtUtc { get; set; }
}
