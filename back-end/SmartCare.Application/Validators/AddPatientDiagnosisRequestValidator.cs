using FluentValidation;
using SmartCare.Application.DTOs.Requests;

namespace SmartCare.Application.Validators;

public sealed class AddPatientDiagnosisRequestValidator : AbstractValidator<AddPatientDiagnosisRequest>
{
    public AddPatientDiagnosisRequestValidator()
    {
        RuleFor(x => x.IcdCode).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
    }
}
