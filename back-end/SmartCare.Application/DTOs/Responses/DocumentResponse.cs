namespace SmartCare.Application.DTOs.Responses;

public sealed class DocumentResponse
{
    public Guid DocumentId { get; init; }

    public string OriginalFileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public int SizeBytes { get; init; }

    public string? EntityType { get; init; }

    public string? EntityId { get; init; }

    public string? UploadedByUserId { get; init; }

    public DateTimeOffset UploadedAtUtc { get; init; }
}
