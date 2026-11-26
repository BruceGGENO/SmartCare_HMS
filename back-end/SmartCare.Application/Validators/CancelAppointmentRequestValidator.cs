using FluentValidation;
using SmartCare.Application.DTOs.Requests;

namespace SmartCare.Application.Validators;

public sealed class CancelAppointmentRequestValidator : AbstractValidator<CancelAppointmentRequest>
{
    public CancelAppointmentRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
