using System.Data.Common;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.FileStorage;

public sealed class LocalFileStorage : IFileStorage
{
    public const string InsertProcedure = "dbo.sp_InsertDocument";
    public const string GetProcedure = "dbo.sp_GetDocumentById";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png"
    };

    private readonly ISqlExecutor _sql;
    private readonly string _rootPath;
    private readonly int _maxFileBytes;

    public LocalFileStorage(ISqlExecutor sql, string rootPath, int maxFileBytes)
    {
        _sql = sql;
        _rootPath = rootPath;
        _maxFileBytes = maxFileBytes;
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredDocument> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        string? entityType,
        string? entityId,
        string? uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var safeName = Path.GetFileName(originalFileName);
        if (string.IsNullOrWhiteSpace(safeName))
            throw new ArgumentException("A file name is required.", nameof(originalFileName));

        var extension = Path.GetExtension(safeName);
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("Only PDF, JPG and PNG files are allowed.", nameof(originalFileName));

        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType))
            throw new ArgumentException("File type is not allowed.", nameof(contentType));

        if (content.CanSeek && content.Length > _maxFileBytes)
            throw new ArgumentException($"File exceeds the {_maxFileBytes / (1024 * 1024)} MB limit.", nameof(content));

        var documentId = Guid.NewGuid();
        var storedFileName = documentId.ToString("N") + extension.ToLowerInvariant();
        var fullPath = Path.Combine(_rootPath, storedFileName);

        var sizeBytes = 0;
        try
        {
            await using (var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    sizeBytes += read;
                    if (sizeBytes > _maxFileBytes)
                        throw new ArgumentException($"File exceeds the {_maxFileBytes / (1024 * 1024)} MB limit.", nameof(content));

                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }

            if (sizeBytes == 0)
                throw new ArgumentException("The uploaded file is empty.", nameof(content));

            var document = new StoredDocument
            {
                DocumentId = documentId,
                OriginalFileName = Truncate(safeName, 255) ?? safeName,
                ContentType = contentType,
                SizeBytes = sizeBytes,
                StoredFileName = storedFileName,
                EntityType = Truncate(entityType, 100),
                EntityId = Truncate(entityId, 64),
                UploadedByUserId = Truncate(uploadedByUserId, 64),
                UploadedAtUtc = DateTimeOffset.UtcNow
            };

            await _sql.ExecuteNonQueryAsync(InsertProcedure, ToParameters(document), cancellationToken);
            return document;
        }
        catch
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            throw;
        }
    }

    public async Task<StoredDocument?> GetMetadataAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _sql.QueryAsync(
            GetProcedure,
            Map,
            new Dictionary<string, object?> { ["DocumentId"] = documentId },
            cancellationToken);

        return rows.Count == 0 ? null : rows[0];
    }

    public async Task<Stream> OpenReadAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await GetMetadataAsync(documentId, cancellationToken)
            ?? throw new FileNotFoundException("Document was not found.", documentId.ToString());

        var fullPath = Path.Combine(_rootPath, metadata.StoredFileName);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Document file is missing from disk.", fullPath);

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    private static StoredDocument Map(DbDataReader record) => new()
    {
        DocumentId = record.Get<Guid>("DocumentId"),
        OriginalFileName = record.Get<string>("OriginalFileName"),
        ContentType = record.Get<string>("ContentType"),
        SizeBytes = record.Get<int>("SizeBytes"),
        StoredFileName = record.Get<string>("StoredFileName"),
        EntityType = record.GetStringOrNull("EntityType"),
        EntityId = record.GetStringOrNull("EntityId"),
        UploadedByUserId = record.GetStringOrNull("UploadedByUserId"),
        UploadedAtUtc = ReadTime(record, "UploadedAtUtc")
    };

    private static Dictionary<string, object?> ToParameters(StoredDocument document) => new()
    {
        ["DocumentId"] = document.DocumentId,
        ["OriginalFileName"] = document.OriginalFileName,
        ["ContentType"] = document.ContentType,
        ["SizeBytes"] = document.SizeBytes,
        ["StoredFileName"] = document.StoredFileName,
        ["EntityType"] = document.EntityType,
        ["EntityId"] = document.EntityId,
        ["UploadedByUserId"] = document.UploadedByUserId,
        ["UploadedAtUtc"] = document.UploadedAtUtc
    };

    private static DateTimeOffset ReadTime(DbDataReader record, string column)
    {
        var value = record.GetValue(record.GetOrdinal(column));
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => record.Get<DateTimeOffset>(column)
        };
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        value = value.Trim();
        return value.Length <= max ? value : value[..max];
    }
}
