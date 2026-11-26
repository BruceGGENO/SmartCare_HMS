namespace SmartCare.Application.DTOs.Requests;

public sealed class CancelAppointmentRequest
{
    public string Reason { get; set; } = string.Empty;
}
