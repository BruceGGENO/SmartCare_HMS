using FluentValidation;
using SmartCare.Application.DTOs.Requests;

namespace SmartCare.Application.Validators;

public sealed class ConsumePackageSessionRequestValidator : AbstractValidator<ConsumePackageSessionRequest>
{
    public ConsumePackageSessionRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(200).When(x => x.Notes is not null);
    }
}
