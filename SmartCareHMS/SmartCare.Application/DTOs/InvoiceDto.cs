namespace SmartCare.Application.DTOs;

public class InvoiceDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty; // Medical Record Number
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance => TotalAmount - PaidAmount; // Amount remaining to pay
    public string Status { get; set; } = "Pending";     // "Pending" or "Paid"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // The list of items on this invoice:
    public List<InvoiceLineDto> Lines { get; set; } = new();
}