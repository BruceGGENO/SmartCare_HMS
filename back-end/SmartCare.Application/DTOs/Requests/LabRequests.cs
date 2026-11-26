namespace SmartCare.Application.DTOs.Requests;

public sealed class CreateLabOrderRequest
{
    public int PatientId { get; set; }
    public int LabTestId { get; set; }
}

public sealed class FileLabResultRequest
{
    public string ResultValue { get; set; } = string.Empty;
    public string? ResultNotes { get; set; }
}
