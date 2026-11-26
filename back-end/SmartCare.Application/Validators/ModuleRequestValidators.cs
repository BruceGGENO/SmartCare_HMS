using FluentValidation;
using SmartCare.Application.DTOs.Requests;

namespace SmartCare.Application.Validators;

public sealed class CreateLabOrderRequestValidator : AbstractValidator<CreateLabOrderRequest>
{
    public CreateLabOrderRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.LabTestId).GreaterThan(0);
    }
}

public sealed class FileLabResultRequestValidator : AbstractValidator<FileLabResultRequest>
{
    public FileLabResultRequestValidator()
    {
        RuleFor(x => x.ResultValue).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ResultNotes).MaximumLength(500).When(x => x.ResultNotes is not null);
    }
}

public sealed class CreatePrescriptionRequestValidator : AbstractValidator<CreatePrescriptionRequest>
{
    public CreatePrescriptionRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.DrugId).GreaterThan(0);
        RuleFor(x => x.Dose).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class AdmitPatientRequestValidator : AbstractValidator<AdmitPatientRequest>
{
    public AdmitPatientRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.BedId).GreaterThan(0);
    }
}

public sealed class TransferAdmissionRequestValidator : AbstractValidator<TransferAdmissionRequest>
{
    public TransferAdmissionRequestValidator() => RuleFor(x => x.NewBedId).GreaterThan(0);
}

public sealed class InitiateDischargeRequestValidator : AbstractValidator<InitiateDischargeRequest>
{
    public InitiateDischargeRequestValidator() => RuleFor(x => x.AdmissionId).GreaterThan(0);
}

public sealed class EnterWaitingQueueRequestValidator : AbstractValidator<EnterWaitingQueueRequest>
{
    public EnterWaitingQueueRequestValidator() =>
        RuleFor(x => x.Station).NotEmpty().MaximumLength(30);
}
