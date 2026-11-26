namespace SmartCare.Application.DTOs;

public sealed class BedDto
{
    public Guid Id { get; set; }
    public string WardName { get; set; } = string.Empty; // ICU, General Medical, Surgical Ward, Maternity, Pediatrics
    public string RoomNumber { get; set; } = string.Empty;
    public string BedNumber { get; set; } = string.Empty;
    public string BedType { get; set; } = "General"; // ICU, General, OT, Step-down
    public string Status { get; set; } = "Available"; // Available, Reserved, Occupied, Cleaning, Out-of-service
    public Guid? CurrentPatientId { get; set; }
    public string? CurrentPatientName { get; set; }
    public string? CurrentPatientMrn { get; set; }
    public DateTimeOffset? OccupiedSinceUtc { get; set; }
    public DateTimeOffset? LastCleanedAtUtc { get; set; }
}

public sealed class CreateBedTransferRequest
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public string FromUnit { get; set; } = "Operating Theatre (OT)";
    public string ToUnit { get; set; } = "Intensive Care Unit (ICU)";
    public string RequestedBedType { get; set; } = "ICU";
    public string Priority { get; set; } = "Urgent"; // Routine, Urgent, STAT
    public string ClinicalNotes { get; set; } = string.Empty;
}

public sealed class BedTransferRequestDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public string FromUnit { get; set; } = string.Empty;
    public string ToUnit { get; set; } = string.Empty;
    public string RequestedBedType { get; set; } = string.Empty;
    public string Priority { get; set; } = "Routine";
    public string Status { get; set; } = "Requested"; // Requested, BedAssigned, Dispatched, Received, Completed
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset? BedAssignedAtUtc { get; set; }
    public DateTimeOffset? DispatchedAtUtc { get; set; }
    public DateTimeOffset? ReceivedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public Guid? AssignedBedId { get; set; }
    public string? AssignedBedNumber { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string? ClinicalNotes { get; set; }
    public double? TurnaroundMinutes => CompletedAtUtc.HasValue 
        ? (CompletedAtUtc.Value - RequestedAtUtc).TotalMinutes 
        : null;
}

public sealed class AssignBedRequest
{
    public Guid BedId { get; set; }
}

public sealed class UpdateBedStageRequest
{
    public string Stage { get; set; } = "Dispatched"; // Dispatched, Received, Completed
}

public sealed class BedTurnaroundReportDto
{
    public string Unit { get; set; } = string.Empty;
    public int TotalTransfers { get; set; }
    public double AverageTurnaroundMinutes { get; set; }
    public double MedianTurnaroundMinutes { get; set; }
    public double P90TurnaroundMinutes { get; set; }
}
