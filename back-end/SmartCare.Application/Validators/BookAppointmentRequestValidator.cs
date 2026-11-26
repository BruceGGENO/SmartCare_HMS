using FluentValidation;
using SmartCare.Application.DTOs.Requests;

namespace SmartCare.Application.Validators;

public sealed class BookAppointmentRequestValidator : AbstractValidator<BookAppointmentRequest>
{
    public BookAppointmentRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.DoctorUserId).NotEmpty();
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ScheduledAtUtc).NotEmpty();
    }
}
