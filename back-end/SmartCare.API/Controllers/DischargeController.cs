using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.DTOs;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPermissions.DischargeRead)]
public sealed class DischargeController : ControllerBase
{
    private readonly IDischargeService _dischargeService;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;

    public DischargeController(
        IDischargeService dischargeService,
        ICurrentUser currentUser,
        IAuditLogger audit)
    {
        _dischargeService = dischargeService;
        _currentUser = currentUser;
        _audit = audit;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DischargeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveDischarges(CancellationToken cancellationToken)
    {
        var discharges = await _dischargeService.GetActiveDischargesAsync(cancellationToken);
        return Ok(discharges);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DischargeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDischargeById(Guid id, CancellationToken cancellationToken)
    {
        var discharge = await _dischargeService.GetDischargeByIdAsync(id, cancellationToken);
        return discharge is null ? NotFound() : Ok(discharge);
    }

    [HttpPost("initiate")]
    [Authorize(Policy = AppPermissions.DischargeManage)]
    [ProducesResponseType(typeof(DischargeDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> InitiateDischarge(
        [FromBody] InitiateDischargeRequest request,
        CancellationToken cancellationToken)
    {
        var doctorId = Guid.TryParse(_currentUser.UserId, out var id) ? id : Guid.NewGuid();
        var discharge = await _dischargeService.InitiateDischargeAsync(request, doctorId, cancellationToken);

        await _audit.LogAsync(new AuditEvent
        {
            Action = "Discharge.Initiate",
            EntityType = "Discharge",
            EntityId = discharge.Id.ToString(),
            ActorUserId = _currentUser.UserId,
            Outcome = "Success"
        }, cancellationToken);

        return CreatedAtAction(nameof(GetDischargeById), new { id = discharge.Id }, discharge);
    }

    [HttpPost("{id:guid}/finalize")]
    [Authorize(Policy = AppPermissions.DischargeManage)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FinalizeDischarge(Guid id, CancellationToken cancellationToken)
    {
        var success = await _dischargeService.FinalizeDischargeAsync(id, cancellationToken);
        if (!success) return BadRequest(new ApiMessageResponse { Message = "Discharge not found or already finalized." });

        await _audit.LogAsync(new AuditEvent
        {
            Action = "Discharge.Finalize",
            EntityType = "Discharge",
            EntityId = id.ToString(),
            ActorUserId = _currentUser.UserId,
            Outcome = "Success"
        }, cancellationToken);

        return Ok(new ApiMessageResponse { Message = "Discharge finalized; clearance checklist initialized." });
    }

    [HttpPost("clearances/{clearanceId:guid}")]
    [Authorize(Policy = AppPermissions.DischargeManage)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkClearance(
        Guid clearanceId,
        [FromBody] MarkClearanceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.TryParse(_currentUser.UserId, out var uid) ? uid : Guid.NewGuid();
        var success = await _dischargeService.MarkClearanceAsync(clearanceId, request.Status, request.BlockReason, userId, cancellationToken);
        if (!success) return BadRequest(new ApiMessageResponse { Message = "Clearance item not found." });

        await _audit.LogAsync(new AuditEvent
        {
            Action = "Discharge.Clearance",
            EntityType = "DischargeClearance",
            EntityId = clearanceId.ToString(),
            ActorUserId = _currentUser.UserId,
            Outcome = "Success"
        }, cancellationToken);

        return Ok(new ApiMessageResponse { Message = $"Clearance updated to {request.Status}." });
    }

    [HttpPost("{id:guid}/addendum")]
    [Authorize(Policy = AppPermissions.DischargeManage)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddAddendum(
        Guid id,
        [FromBody] AddendumRequest request,
        CancellationToken cancellationToken)
    {
        var staffId = Guid.TryParse(_currentUser.UserId, out var uid) ? uid : Guid.NewGuid();
        var success = await _dischargeService.AddAddendumAsync(id, request.Text, staffId, cancellationToken);
        if (!success) return BadRequest(new ApiMessageResponse { Message = "Discharge not found." });

        return Ok(new ApiMessageResponse { Message = "Addendum added successfully." });
    }
}
