using SmartCare.Application.Interfaces.Services;

namespace SmartCare.Infrastructure.Services;

public class PaymentGatewayStub : IPaymentGateway
{
    public async Task<PaymentResult> ChargeAsync(
        decimal amount, 
        string paymentMethod, 
        string phoneNumber = "", 
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return new PaymentResult
            {
                IsSuccess = false,
                Message = "Amount must be greater than zero."
            };
        }

        // Simulate network processing delay (200ms)
        await Task.Delay(200, cancellationToken);

        var reference = $"TXN-{Random.Shared.Next(100000, 999999)}";
        var phoneDisplay = string.IsNullOrWhiteSpace(phoneNumber) ? "" : $" (Phone: {phoneNumber})";

        return new PaymentResult
        {
            IsSuccess = true,
            TransactionReference = reference,
            Message = $"Payment of {amount:F2} via {paymentMethod}{phoneDisplay} confirmed."
        };
    }
}
