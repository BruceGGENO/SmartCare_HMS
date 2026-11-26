namespace SmartCare.Application.DTOs.Responses;

public sealed class PatientResponse
{
    public int PatientId { get; set; }

    public Guid? UserId { get; set; }

    public string Mrn { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public string? Phone { get; set; }

    public string? Sex { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
