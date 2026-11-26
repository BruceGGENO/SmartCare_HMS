namespace SmartCare.Application.DTOs.Requests;

public sealed class PayInvoiceRequest
{
    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = "MobileMoney";

    public string? PhoneNumber { get; set; }
}
