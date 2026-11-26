namespace SmartCare.Application.DTOs;

public sealed class LabCatalogItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Biochemistry, Hematology, Microbiology, Radiology
    public decimal UnitPrice { get; set; }
    public string ReferenceRange { get; set; } = string.Empty;
    public string SpecimenType { get; set; } = "Blood"; // Blood, Urine, Swab, Imaging
    public int TurnaroundHours { get; set; } = 4;
}

public sealed class CreateLabOrderRequest
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid TestCatalogItemId { get; set; }
    public string Priority { get; set; } = "Routine"; // Routine, Urgent, STAT
    public string ClinicalNotes { get; set; } = string.Empty;
}

public sealed class LabOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = "Routine";
    public string SampleStatus { get; set; } = "Collected"; // Collected, Received, In-process, Completed
    public string SampleId { get; set; } = string.Empty;
    public string OrderedBy { get; set; } = string.Empty;
    public DateTimeOffset OrderedAtUtc { get; set; }
    public string? ResultValue { get; set; }
    public string? ReferenceRange { get; set; }
    public string? Unit { get; set; }
    public bool IsAbnormal { get; set; }
    public bool IsCritical { get; set; }
    public bool IsVerified { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public string? TechnicianNotes { get; set; }
}

public sealed class UpdateSampleStatusRequest
{
    public string Status { get; set; } = "Received"; // Received, In-process, Completed
    public string? SampleBarcode { get; set; }
}

public sealed class SubmitLabResultRequest
{
    public string ResultValue { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public bool IsAbnormal { get; set; }
    public bool IsCritical { get; set; }
    public string? TechnicianNotes { get; set; }
}
