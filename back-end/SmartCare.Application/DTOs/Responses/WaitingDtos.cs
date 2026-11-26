namespace SmartCare.Application.DTOs.Responses;

public sealed class WaitingQueueItemResponse
{
    public int AppointmentId { get; set; }
    public Guid PublicId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public Guid DoctorUserId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CheckInCode { get; set; } = string.Empty;
    public string? QueueStation { get; set; }
    public DateTime? QueueEnteredAtUtc { get; set; }
    public DateTime? CalledAtUtc { get; set; }
}
