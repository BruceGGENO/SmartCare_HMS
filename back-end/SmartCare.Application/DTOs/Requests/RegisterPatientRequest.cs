namespace SmartCare.Application.DTOs.Requests;

public sealed class RegisterPatientRequest
{
    public string Mrn { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public string? Phone { get; set; }

    public string? Sex { get; set; }

    public Guid? UserId { get; set; }
}
