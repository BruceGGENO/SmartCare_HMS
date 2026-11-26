namespace SmartCare.Application.DTOs.Responses;

public sealed class DrugResponse
{
    public int DrugId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public int StockQty { get; set; }
}

public sealed class PrescriptionResponse
{
    public int PrescriptionId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public int DrugId { get; set; }
    public string DrugCode { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string Dose { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Guid? PrescribedByUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset PrescribedAtUtc { get; set; }
    public DateTimeOffset? DispensedAtUtc { get; set; }
    public Guid? DispensedByUserId { get; set; }
}
