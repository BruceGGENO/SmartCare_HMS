namespace SmartCare.Application.Interfaces.Services;

public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(
        decimal amount,
        string paymentMethod,
        string? phoneNumber = null,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentResult
{
    public bool IsSuccess { get; init; }

    public string TransactionReference { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public static PaymentResult Success(string reference, string message) =>
        new() { IsSuccess = true, TransactionReference = reference, Message = message };

    public static PaymentResult Fail(string message) =>
        new() { IsSuccess = false, Message = message };
}
