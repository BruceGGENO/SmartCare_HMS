using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;

namespace SmartCare.Presentation.Controllers
{
    // Add [Authorize] here after Raissa's login is on dev so downloads are not public.
    public class DocumentsController : Controller
    {
        private const long RequestLimitBytes = 6 * 1024 * 1024;

        private readonly IFileStorage _files;
        private readonly IAuditLogger _audit;

        public DocumentsController(IFileStorage files, IAuditLogger audit)
        {
            _files = files;
            _audit = audit;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(RequestLimitBytes)]
        public async Task<IActionResult> Upload(
            IFormFile? file,
            string? entityType,
            string? entityId,
            CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Choose a PDF, JPG or PNG file.");
                return View();
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var saved = await _files.SaveAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    entityType,
                    entityId,
                    User.Identity?.Name,
                    cancellationToken);

                await _audit.LogAsync(new AuditEvent
                {
                    Action = "Document.Create",
                    EntityType = string.IsNullOrWhiteSpace(entityType) ? "Document" : entityType,
                    EntityId = entityId ?? saved.DocumentId.ToString(),
                    ActorUserId = User.Identity?.Name,
                    Outcome = "Success"
                }, cancellationToken);

                TempData["Message"] = $"Saved {saved.OriginalFileName} ({saved.DocumentId}).";
                return RedirectToAction(nameof(Upload));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var metadata = await _files.GetMetadataAsync(id, cancellationToken);
                if (metadata is null)
                    return NotFound();

                var stream = await _files.OpenReadAsync(id, cancellationToken);
                try
                {
                    await _audit.LogAsync(new AuditEvent
                    {
                        Action = "Document.Read",
                        EntityType = "Document",
                        EntityId = id.ToString(),
                        ActorUserId = User.Identity?.Name,
                        Outcome = "Success"
                    }, cancellationToken);
                }
                catch
                {
                    await stream.DisposeAsync();
                    throw;
                }

                return File(stream, metadata.ContentType, metadata.OriginalFileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
