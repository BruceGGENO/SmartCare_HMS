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
[Authorize(Policy = AppPermissions.WaitingRoomRead)]
public sealed class WaitingRoomController : ControllerBase
{
    private readonly IWaitingRoomService _waitingRoomService;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;

    public WaitingRoomController(
        IWaitingRoomService waitingRoomService,
        ICurrentUser currentUser,
        IAuditLogger audit)
    {
        _waitingRoomService = waitingRoomService;
        _currentUser = currentUser;
        _audit = audit;
    }

    [HttpGet("live-queue")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientJourneyEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLiveQueue([FromQuery] string? station, CancellationToken cancellationToken)
    {
        var queue = await _waitingRoomService.GetLiveQueueAsync(station, cancellationToken);
        return Ok(queue);
    }

    [HttpPost("events")]
    [Authorize(Policy = AppPermissions.WaitingRoomManage)]
    [ProducesResponseType(typeof(PatientJourneyEventDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordEvent(
        [FromBody] RecordJourneyEventRequest request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.TryParse(_currentUser.UserId, out var uid) ? uid : (Guid?)null;
        var evt = await _waitingRoomService.RecordEventAsync(request, userId, cancellationToken);

        await _audit.LogAsync(new AuditEvent
        {
            Action = "WaitingRoom.RecordEvent",
            EntityType = "PatientJourney",
            EntityId = evt.Id.ToString(),
            ActorUserId = _currentUser.UserId,
            Outcome = "Success"
        }, cancellationToken);

        return CreatedAtAction(nameof(GetLiveQueue), new { station = evt.Station }, evt);
    }

    [HttpGet("analytics")]
    [ProducesResponseType(typeof(IReadOnlyList<StationWaitAnalyticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] double targetMinutes = 30,
        CancellationToken cancellationToken = default)
    {
        var analytics = await _waitingRoomService.GetAnalyticsAsync(from, to, targetMinutes, cancellationToken);
        return Ok(analytics);
    }

    [HttpGet("heatmap")]
    [ProducesResponseType(typeof(IReadOnlyList<HeatmapCellDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHeatmap(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var heatmap = await _waitingRoomService.GetHeatmapAsync(from, to, cancellationToken);
        return Ok(heatmap);
    }

    [HttpPost("correct")]
    [Authorize(Policy = AppPermissions.WaitingRoomManage)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CorrectEvent(
        [FromBody] CorrectJourneyEventRequest request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.TryParse(_currentUser.UserId, out var uid) ? uid : (Guid?)null;
        var success = await _waitingRoomService.CorrectEventAsync(request, userId, cancellationToken);
        if (!success) return BadRequest(new ApiMessageResponse { Message = "Original event not found." });

        await _audit.LogAsync(new AuditEvent
        {
            Action = "WaitingRoom.CorrectEvent",
            EntityType = "PatientJourney",
            EntityId = request.EventId.ToString(),
            ActorUserId = _currentUser.UserId,
            Outcome = "Success"
        }, cancellationToken);

        return Ok(new ApiMessageResponse { Message = "Event correction recorded." });
    }
}
