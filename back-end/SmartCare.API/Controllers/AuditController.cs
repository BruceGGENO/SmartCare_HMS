using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.HospitalAdmin}")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditLogger _audit;

    public AuditController(IAuditLogger audit)
    {
        _audit = audit;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var rows = await _audit.ListRecentAsync(take, cancellationToken);
        var response = rows.Select(r => new AuditLogResponse
        {
            AuditLogId = r.AuditLogId,
            Action = r.Action,
            EntityType = r.EntityType,
            EntityId = r.EntityId,
            ActorUserId = r.ActorUserId,
            Outcome = r.Outcome,
            OccurredAtUtc = r.OccurredAtUtc,
            CorrelationId = r.CorrelationId
        }).ToArray();

        return Ok(response);
    }
}
