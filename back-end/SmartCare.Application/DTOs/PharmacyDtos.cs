namespace SmartCare.Application.DTOs;

public sealed class DrugCatalogDto
{
    public Guid Id { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DosageForm { get; set; } = "Tablet"; // Tablet, Syrup, Injection, Capsule
    public string Strength { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int MinimumStockThreshold { get; set; }
    public int TotalStock { get; set; }
    public bool IsLowStock => TotalStock <= MinimumStockThreshold;
    public List<StockBatchDto> Batches { get; set; } = [];
}

public sealed class StockBatchDto
{
    public string BatchNumber { get; set; } = string.Empty;
    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public bool IsExpired => DateTime.UtcNow.Date > ExpiryDate.Date;
    public bool IsNearExpiry => !IsExpired && (ExpiryDate.Date - DateTime.UtcNow.Date).TotalDays <= 60;
}

public sealed class DispenseMedicationRequest
{
    public Guid DrugId { get; set; }
    public Guid? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid? PrescriptionId { get; set; }
    public int Quantity { get; set; }
    public string? AllergyOverrideReason { get; set; }
}

public sealed class DispenseResultDto
{
    public Guid Id { get; set; }
    public string DispenseNumber { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string DispensedBy { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public decimal BilledAmount { get; set; }
    public DateTimeOffset DispensedAtUtc { get; set; }
    public bool BilledToInvoice { get; set; } = true;
}

public sealed class ReorderAlertDto
{
    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinimumThreshold { get; set; }
    public int SuggestedReorderQty { get; set; }
    public string Urgency { get; set; } = "Medium"; // Low, Medium, Critical
}
