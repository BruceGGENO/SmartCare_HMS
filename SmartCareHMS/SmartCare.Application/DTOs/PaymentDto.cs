namespace SmartCare.Application.DTOs;

public class PaymentDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, MobileMoney
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public string TransactionReference { get; set; } = string.Empty;
}