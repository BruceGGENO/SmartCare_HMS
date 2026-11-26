namespace SmartCare.Application.DTOs.Requests;

public sealed class EnterWaitingQueueRequest
{
    public string Station { get; set; } = "Triage";
}
