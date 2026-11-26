using SmartCare.Application.Interfaces.Services;

namespace SmartCare.Infrastructure.Services;

public sealed class PaymentGatewayStub : IPaymentGateway
{
    public async Task<PaymentResult> ChargeAsync(
        decimal amount,
        string paymentMethod,
        string? phoneNumber = null,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            return PaymentResult.Fail("Amount must be greater than zero.");

        await Task.Delay(150, cancellationToken);

        var reference = $"TXN-{Random.Shared.Next(100000, 999999)}";
        var phonePart = string.IsNullOrWhiteSpace(phoneNumber) ? string.Empty : $" (Phone: {phoneNumber})";

        return PaymentResult.Success(
            reference,
            $"Payment of {amount:F2} via {paymentMethod}{phonePart} confirmed (stub).");
    }
}
