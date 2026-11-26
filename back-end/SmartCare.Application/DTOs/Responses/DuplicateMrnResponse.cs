namespace SmartCare.Application.DTOs.Responses;

public sealed class DuplicateMrnResponse
{
    public string Message { get; init; } = "A patient with this MRN already exists.";

    public PatientResponse ExistingPatient { get; init; } = null!;
}
