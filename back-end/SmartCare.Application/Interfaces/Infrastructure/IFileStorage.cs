namespace SmartCare.Application.Interfaces.Infrastructure;

public interface IFileStorage
{
    Task<StoredDocument> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        string? entityType,
        string? entityId,
        string? uploadedByUserId,
        CancellationToken cancellationToken = default);

    Task<StoredDocument?> GetMetadataAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}
