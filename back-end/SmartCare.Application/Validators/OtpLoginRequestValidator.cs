using FluentValidation;
using SmartCare.Application.DTOs.Requests;

namespace SmartCare.Application.Validators;

public sealed class OtpLoginRequestValidator : AbstractValidator<OtpLoginRequest>
{
    public OtpLoginRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}
