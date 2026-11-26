namespace SmartCare.Application.DTOs.Responses;

public sealed class InvoiceResponse
{
    public int InvoiceId { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string PatientMrn { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal Balance => TotalAmount - PaidAmount;

    public string Status { get; set; } = "Pending";

    public DateTimeOffset CreatedAtUtc { get; set; }

    public IReadOnlyList<InvoiceLineResponse> Lines { get; set; } = [];

    public IReadOnlyList<PaymentResponse> Payments { get; set; } = [];
}
