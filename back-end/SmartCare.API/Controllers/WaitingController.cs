using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.DTOs.Requests;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class WaitingController : ControllerBase
{
    private readonly IWaitingService _waiting;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;
    private readonly IValidator<EnterWaitingQueueRequest> _enterValidator;

    public WaitingController(
        IWaitingService waiting, ICurrentUser currentUser, IAuditLogger audit,
        IValidator<EnterWaitingQueueRequest> enterValidator)
    {
        _waiting = waiting;
        _currentUser = currentUser;
        _audit = audit;
        _enterValidator = enterValidator;
    }

    [HttpGet("queue")]
    [Authorize(Policy = AppPermissions.WaitingRead)]
    public async Task<IActionResult> Queue([FromQuery] string? department, [FromQuery] string? station, CancellationToken ct) =>
        Ok(await _waiting.ListQueueAsync(department, station, ct));

    [HttpPost("appointments/{id:int}/enter")]
    [Authorize(Policy = AppPermissions.WaitingManage)]
    public async Task<IActionResult> Enter(int id, [FromBody] EnterWaitingQueueRequest? request, CancellationToken ct)
    {
        request ??= new EnterWaitingQueueRequest();
        var validation = await _enterValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var item = await _waiting.EnterQueueAsync(id, request.Station, ct);
            await _audit.LogAsync(new AuditEvent
            {
                Action = "Waiting.Enter",
                EntityType = "Appointment",
                EntityId = id.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, ct);
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiMessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("appointments/{id:int}/call")]
    [Authorize(Policy = AppPermissions.WaitingManage)]
    public async Task<IActionResult> Call(int id, CancellationToken ct)
    {
        try
        {
            var item = await _waiting.CallAsync(id, ct);
            await _audit.LogAsync(new AuditEvent
            {
                Action = "Waiting.Call",
                EntityType = "Appointment",
                EntityId = id.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, ct);
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiMessageResponse { Message = ex.Message });
        }
    }
}
