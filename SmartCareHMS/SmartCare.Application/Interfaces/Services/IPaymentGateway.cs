namespace SmartCare.Application.Interfaces.Services;

public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(
        decimal amount, 
        string paymentMethod, 
        string phoneNumber = "", 
        CancellationToken cancellationToken = default);
}

public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
