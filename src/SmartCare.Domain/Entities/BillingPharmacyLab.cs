using SmartCare.Domain.Common;
namespace SmartCare.Domain.Entities;

// ---------------- Billing ----------------
public class PriceItem : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PriceCategory Category { get; set; }
    public decimal UnitPrice { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

public class Invoice : BaseEntity, IPhiEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid? EncounterId { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public string? DiscountReason { get; set; }
    public Guid? DiscountApprovedByUserId { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public decimal InsurerPortion { get; set; }
    public decimal PatientPortion { get; set; }
    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class InvoiceLine : BaseEntity, IPhiEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public Guid? PriceItemId { get; set; }
    public Guid? ServiceEventId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }                    // copied at invoice time so later price changes never alter it
    public decimal LineTotal { get; set; }
}

public class Payment : BaseEntity, IPhiEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; }                    // MoMo / bank reference
    public string? GatewayTransactionId { get; set; }         // unique, makes gateway callbacks idempotent
    public Guid? ReceivedByUserId { get; set; }
}

public class InsuranceClaim : BaseEntity, IPhiEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public Guid InsuranceCoverageId { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Draft;
    public decimal ClaimedAmount { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}

public class Package : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public decimal TotalPrice { get; set; }
    public int? ValidDays { get; set; }
}

public class PackagePurchase : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid PackageId { get; set; }
    public Package Package { get; set; } = null!;
    public int SessionsPurchased { get; set; }
    public int SessionsConsumed { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime PurchasedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
}

// ---------------- Pharmacy ----------------
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class Drug : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Form { get; set; }
    public string? Strength { get; set; }
    public int ReorderLevel { get; set; }
    public int ReorderQuantity { get; set; }
    public string? DrugClass { get; set; }                    // e.g. Penicillin: used by the allergy check
    public bool ReorderAlertActive { get; set; }              // true while a low-stock breach is open (alert fires once per breach)
    public bool IsActive { get; set; } = true;
    public Guid? PriceItemId { get; set; }
}

public class StockBatch : BaseEntity, IConcurrent
{
    public Guid DrugId { get; set; }
    public Drug Drug { get; set; } = null!;
    public string StoreName { get; set; } = "Main Pharmacy"; // pharmacy or ward store
    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public int QuantityOnHand { get; set; }
    public decimal UnitCost { get; set; }
    public Guid? SupplierId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class StockMovement : BaseEntity
{
    public Guid StockBatchId { get; set; }
    public StockBatch StockBatch { get; set; } = null!;
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }                         // signed: receipts positive, dispensing negative
    public string? Reason { get; set; }
    public Guid? ByUserId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public class Prescription : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid EncounterId { get; set; }
    public Guid DoctorId { get; set; }
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Active;
    public string? AllergyOverrideReason { get; set; }
    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}

public class PrescriptionItem : BaseEntity, IPhiEntity, IConcurrent
{
    public Guid PrescriptionId { get; set; }
    public Prescription Prescription { get; set; } = null!;
    public Guid PatientId { get; set; }
    public Guid DrugId { get; set; }
    public Drug Drug { get; set; } = null!;
    public string Dose { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string? Duration { get; set; }
    public int QuantityPrescribed { get; set; }
    public int QuantityDispensed { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class DispenseRecord : BaseEntity, IPhiEntity
{
    public Guid PrescriptionItemId { get; set; }
    public PrescriptionItem PrescriptionItem { get; set; } = null!;
    public Guid PatientId { get; set; }
    public Guid StockBatchId { get; set; }
    public StockBatch StockBatch { get; set; } = null!;
    public int Quantity { get; set; }
    public Guid DispensedByUserId { get; set; }
    public DateTime DispensedAtUtc { get; set; } = DateTime.UtcNow;
    public string? AllergyOverrideReason { get; set; }
}

public class PurchaseOrder : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTime? OrderedAtUtc { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}

public class PurchaseOrderLine : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Guid DrugId { get; set; }
    public Drug Drug { get; set; } = null!;
    public int Quantity { get; set; }
    public int QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
}

// ---------------- Laboratory & Radiology ----------------
public class TestCatalogItem : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TestKind Kind { get; set; }
    public string? Unit { get; set; }
    public decimal? ReferenceLow { get; set; }
    public decimal? ReferenceHigh { get; set; }
    public decimal? CriticalLow { get; set; }
    public decimal? CriticalHigh { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? PriceItemId { get; set; }
}

public class LabOrder : BaseEntity, IPhiEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid EncounterId { get; set; }
    public Guid TestCatalogItemId { get; set; }
    public TestCatalogItem Test { get; set; } = null!;
    public Guid OrderedByStaffId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Ordered;
    public DateTime OrderedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Sample> Samples { get; set; } = new List<Sample>();
    public ICollection<LabResult> Results { get; set; } = new List<LabResult>();
}

public class Sample : BaseEntity, IPhiEntity
{
    public Guid LabOrderId { get; set; }
    public LabOrder LabOrder { get; set; } = null!;
    public Guid PatientId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public SampleStatus Status { get; set; } = SampleStatus.Collected;
    public DateTime? CollectedAtUtc { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public ICollection<SampleStatusHistory> History { get; set; } = new List<SampleStatusHistory>();
}

public class LabResult : BaseEntity, IPhiEntity
{
    public Guid LabOrderId { get; set; }
    public LabOrder LabOrder { get; set; } = null!;
    public Guid PatientId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal? ReferenceLow { get; set; }
    public decimal? ReferenceHigh { get; set; }
    public bool IsAbnormal { get; set; }
    public bool IsCritical { get; set; }
    public Guid? EnteredByUserId { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }              // unverified results are never shown in the portal
}

public class ImagingStudy : BaseEntity, IPhiEntity
{
    public Guid LabOrderId { get; set; }
    public LabOrder LabOrder { get; set; } = null!;
    public Guid PatientId { get; set; }
    public string? FilePath { get; set; }
    public string? DicomLink { get; set; }
    [LongText] public string? ReportText { get; set; }
}

public class SampleStatusHistory : BaseEntity
{
    public Guid SampleId { get; set; }
    public Sample Sample { get; set; } = null!;
    public SampleStatus Status { get; set; }
    public Guid? ByUserId { get; set; }
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>In-app notification (e.g. lab result ready). Critical results must be acknowledged.</summary>
public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool RequiresAcknowledgment { get; set; }
    public bool IsRead { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
}
