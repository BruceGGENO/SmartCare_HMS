namespace SmartCare.Application.DTOs.Responses;

public sealed class LabTestResponse
{
    public int LabTestId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Specimen { get; set; }
}

public sealed class LabOrderResponse
{
    public int LabOrderId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public int LabTestId { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public Guid? OrderedByUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ResultValue { get; set; }
    public string? ResultNotes { get; set; }
    public Guid? ResultedByUserId { get; set; }
    public DateTimeOffset OrderedAtUtc { get; set; }
    public DateTimeOffset? ResultedAtUtc { get; set; }
}
