namespace SmartCare.Application.DTOs.Responses;

public sealed class PortalHistoryResponse
{
    public IReadOnlyList<AppointmentResponse> Appointments { get; init; } = [];

    public IReadOnlyList<ReleasedNoteResponse> Notes { get; init; } = [];
}
