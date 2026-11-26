using FluentValidation;
using SmartCare.Application.DTOs.Requests;

namespace SmartCare.Application.Validators;

public sealed class PayInvoiceRequestValidator : AbstractValidator<PayInvoiceRequest>
{
    private static readonly string[] AllowedMethods = ["MobileMoney", "Cash", "Card"];

    public PayInvoiceRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .Must(m => AllowedMethods.Contains(m, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Payment method must be MobileMoney, Cash, or Card.");
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(30)
            .When(x => string.Equals(x.PaymentMethod, "MobileMoney", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Phone number is required for Mobile Money.");
    }
}
