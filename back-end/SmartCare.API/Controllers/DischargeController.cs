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
public sealed class DischargeController : ControllerBase
{
    private readonly IDischargeService _discharge;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;
    private readonly IValidator<InitiateDischargeRequest> _initiateValidator;

    public DischargeController(
        IDischargeService discharge, ICurrentUser currentUser, IAuditLogger audit,
        IValidator<InitiateDischargeRequest> initiateValidator)
    {
        _discharge = discharge;
        _currentUser = currentUser;
        _audit = audit;
        _initiateValidator = initiateValidator;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.DischargeRead)]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct) =>
        Ok(await _discharge.ListAsync(status, ct));

    [HttpGet("{id:int}")]
    [Authorize(Policy = AppPermissions.DischargeRead)]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var item = await _discharge.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = AppPermissions.DischargeWrite)]
    public async Task<IActionResult> Initiate([FromBody] InitiateDischargeRequest request, CancellationToken ct)
    {
        var validation = await _initiateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var created = await _discharge.InitiateAsync(request.AdmissionId, ct);
            await _audit.LogAsync(new AuditEvent
            {
                Action = "Discharge.Initiate",
                EntityType = "DischargeCase",
                EntityId = created.DischargeId.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, ct);
            return CreatedAtAction(nameof(Get), new { id = created.DischargeId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiMessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("steps/{stepId:int}/complete")]
    [Authorize(Policy = AppPermissions.DischargeWrite)]
    public async Task<IActionResult> CompleteStep(int stepId, CancellationToken ct)
    {
        try
        {
            var step = await _discharge.CompleteStepAsync(stepId, ct);
            await _audit.LogAsync(new AuditEvent
            {
                Action = "Discharge.StepComplete",
                EntityType = "DischargeStep",
                EntityId = stepId.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, ct);
            return Ok(step);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiMessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("{id:int}/complete")]
    [Authorize(Policy = AppPermissions.DischargeWrite)]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        if (await _discharge.GetByIdAsync(id, ct) is null)
            return NotFound();

        try
        {
            var updated = await _discharge.CompleteAsync(id, ct);
            await _audit.LogAsync(new AuditEvent
            {
                Action = "Discharge.Complete",
                EntityType = "DischargeCase",
                EntityId = id.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, ct);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiMessageResponse { Message = ex.Message });
        }
    }
}
