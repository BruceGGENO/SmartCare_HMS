namespace SmartCare.Application.DTOs.Responses;

public sealed class PaymentResponse
{
    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string TransactionReference { get; set; } = string.Empty;

    public DateTimeOffset PaidAtUtc { get; set; }
}
