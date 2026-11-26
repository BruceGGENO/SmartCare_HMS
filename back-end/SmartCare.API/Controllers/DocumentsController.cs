using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DocumentsController : ControllerBase
{
    private const long RequestLimitBytes = 6 * 1024 * 1024;

    private readonly IFileStorage _files;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUser _currentUser;

    public DocumentsController(IFileStorage files, IAuditLogger audit, ICurrentUser currentUser)
    {
        _files = files;
        _audit = audit;
        _currentUser = currentUser;
    }

    [HttpPost]
    [RequestSizeLimit(RequestLimitBytes)]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        [FromForm] string? entityType,
        [FromForm] string? entityId,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiMessageResponse { Message = "Choose a PDF, JPG or PNG file." });

        try
        {
            await using var stream = file.OpenReadStream();
            var saved = await _files.SaveAsync(
                stream,
                file.FileName,
                file.ContentType,
                entityType,
                entityId,
                _currentUser.UserId,
                cancellationToken);

            await _audit.LogAsync(new AuditEvent
            {
                Action = "Document.Create",
                EntityType = string.IsNullOrWhiteSpace(entityType) ? "Document" : entityType,
                EntityId = entityId ?? saved.DocumentId.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, cancellationToken);

            return CreatedAtAction(nameof(GetMetadata), new { id = saved.DocumentId }, ToResponse(saved));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiMessageResponse { Message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMetadata(Guid id, CancellationToken cancellationToken)
    {
        var metadata = await _files.GetMetadataAsync(id, cancellationToken);
        return metadata is null ? NotFound() : Ok(ToResponse(metadata));
    }

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _files.GetMetadataAsync(id, cancellationToken);
            if (metadata is null)
                return NotFound();

            var stream = await _files.OpenReadAsync(id, cancellationToken);

            await _audit.LogAsync(new AuditEvent
            {
                Action = "Document.Read",
                EntityType = "Document",
                EntityId = id.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, cancellationToken);

            return File(stream, metadata.ContentType, metadata.OriginalFileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    private static DocumentResponse ToResponse(StoredDocument document) => new()
    {
        DocumentId = document.DocumentId,
        OriginalFileName = document.OriginalFileName,
        ContentType = document.ContentType,
        SizeBytes = document.SizeBytes,
        EntityType = document.EntityType,
        EntityId = document.EntityId,
        UploadedByUserId = document.UploadedByUserId,
        UploadedAtUtc = document.UploadedAtUtc
    };
}
