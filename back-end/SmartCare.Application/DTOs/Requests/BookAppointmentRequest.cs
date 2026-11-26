namespace SmartCare.Application.DTOs.Requests;

public sealed class BookAppointmentRequest
{
    public int PatientId { get; set; }

    public Guid DoctorUserId { get; set; }

    public string Department { get; set; } = string.Empty;

    public DateTime ScheduledAtUtc { get; set; }
}
