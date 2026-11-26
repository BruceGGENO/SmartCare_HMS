namespace SmartCare.Application.DTOs;

public sealed class DischargeDto
{
    public Guid Id { get; set; }
    public Guid AdmissionId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = "";
    public string PatientMrn { get; set; } = "";
    public Guid DoctorId { get; set; }
    public string Stage { get; set; } = "Draft";
    public string Summary { get; set; } = "";
    public string? Medications { get; set; }
    public string? FollowUpInstructions { get; set; }
    public DateTime? FinalizedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<DischargeClearanceDto> Clearances { get; set; } = new();
}

public sealed class DischargeClearanceDto
{
    public Guid Id { get; set; }
    public Guid DischargeId { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string? BlockReason { get; set; }
    public bool IsMandatory { get; set; }
    public DateTime? ClearedAtUtc { get; set; }
}

public sealed class InitiateDischargeRequest
{
    public Guid AdmissionId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = "";
    public string PatientMrn { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? Medications { get; set; }
    public string? FollowUpInstructions { get; set; }
}

public sealed class MarkClearanceRequest
{
    public string Status { get; set; } = "Cleared"; // Cleared | Blocked
    public string? BlockReason { get; set; }
}

public sealed class AddendumRequest
{
    public string Text { get; set; } = "";
}

public sealed class PatientJourneyEventDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = "";
    public string PatientMrn { get; set; } = "";
    public string Station { get; set; } = "";
    public string EventType { get; set; } = ""; // Enter | Leave
    public DateTime OccurredAtUtc { get; set; }
    public double WaitMinutes { get; set; }
    public bool IsManualCorrection { get; set; }
    public string? CorrectionReason { get; set; }
}

public sealed class RecordJourneyEventRequest
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = "";
    public string PatientMrn { get; set; } = "";
    public string Station { get; set; } = ""; // Registration, Triage, Doctor, Laboratory, Pharmacy, Billing, Radiology, Exit
    public string EventType { get; set; } = "Enter"; // Enter | Leave
}

public sealed class CorrectJourneyEventRequest
{
    public Guid EventId { get; set; }
    public string CorrectedStation { get; set; } = "";
    public string CorrectedEventType { get; set; } = "Enter";
    public DateTime CorrectedTimeUtc { get; set; }
    public string Reason { get; set; } = "";
}

public sealed class StationWaitAnalyticsDto
{
    public string Station { get; set; } = "";
    public double AvgMinutes { get; set; }
    public double MedianMinutes { get; set; }
    public double P90Minutes { get; set; }
    public int EventCount { get; set; }
    public bool IsBottleneck { get; set; }
}

public sealed class HeatmapCellDto
{
    public string Station { get; set; } = "";
    public int HourOfDay { get; set; }
    public double AvgWaitMinutes { get; set; }
}
