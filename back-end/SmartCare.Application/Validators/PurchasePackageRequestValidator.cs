using FluentValidation;
using SmartCare.Application.DTOs.Requests;

namespace SmartCare.Application.Validators;

public sealed class PurchasePackageRequestValidator : AbstractValidator<PurchasePackageRequest>
{
    public PurchasePackageRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.PackageName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TotalSessions).GreaterThan(0);
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0);
    }
}
