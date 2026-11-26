namespace SmartCare.Application.DTOs.Responses;

public sealed class OutboxMessageResponse
{
    public long OutboxId { get; init; }

    public string Channel { get; init; } = string.Empty;

    public string Recipient { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; init; }
}
