namespace SmartCare.Application.DTOs.Responses;

public sealed class DischargeCaseResponse
{
    public int DischargeId { get; set; }
    public int AdmissionId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public IReadOnlyList<DischargeStepResponse> Steps { get; set; } = [];
}

public sealed class DischargeStepResponse
{
    public int DischargeStepId { get; set; }
    public int DischargeId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsComplete { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}
