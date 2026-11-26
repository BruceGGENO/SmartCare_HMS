using FluentValidation;
using SmartCare.Application.DTOs.Requests;

namespace SmartCare.Application.Validators;

public sealed class AddPatientAllergyRequestValidator : AbstractValidator<AddPatientAllergyRequest>
{
    public AddPatientAllergyRequestValidator()
    {
        RuleFor(x => x.Substance).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Reaction).MaximumLength(300).When(x => x.Reaction is not null);
        RuleFor(x => x.Severity).MaximumLength(50).When(x => x.Severity is not null);
    }
}
