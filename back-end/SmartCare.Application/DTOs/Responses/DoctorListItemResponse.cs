namespace SmartCare.Application.DTOs.Responses;

public sealed class DoctorListItemResponse
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}
