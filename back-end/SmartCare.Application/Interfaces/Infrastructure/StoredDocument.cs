namespace SmartCare.Application.Interfaces.Infrastructure;

public sealed class StoredDocument
{
    public Guid DocumentId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public int SizeBytes { get; set; }

    public string StoredFileName { get; set; } = string.Empty;

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? UploadedByUserId { get; set; }

    public DateTimeOffset UploadedAtUtc { get; set; }
}
