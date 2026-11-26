using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BedsController : ControllerBase
{
    private readonly IBedOrchestratorService _beds;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;

    public BedsController(
        IBedOrchestratorService beds,
        ICurrentUser currentUser,
        IAuditLogger audit)
    {
        _beds = beds;
        _currentUser = currentUser;
        _audit = audit;
    }

    /// <summary>
    /// Live bed inventory by ward and status (FR-BED-001).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AppPermissions.BedsRead)]
    public async Task<IActionResult> GetBedInventory([FromQuery] string? ward, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var inventory = await _beds.GetBedInventoryAsync(ward, status, cancellationToken);
        return Ok(inventory);
    }

    /// <summary>
    /// Active bed transfer requests with stage timestamps (FR-BED-002, FR-BED-003).
    /// </summary>
    [HttpGet("transfers")]
    [Authorize(Policy = AppPermissions.BedsRead)]
    public async Task<IActionResult> GetTransferRequests([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var requests = await _beds.GetTransferRequestsAsync(status, cancellationToken);
        return Ok(requests);
    }

    /// <summary>
    /// Raise bed transfer/reservation request (e.g. OT -> ICU) (FR-BED-002).
    /// </summary>
    [HttpPost("transfers")]
    [Authorize(Policy = AppPermissions.BedsManage)]
    public async Task<IActionResult> RequestTransfer([FromBody] CreateBedTransferRequest request, CancellationToken cancellationToken)
    {
        var requestedBy = _currentUser.UserName ?? "Nurse / Doctor";
        var transfer = await _beds.RequestTransferAsync(request, requestedBy, cancellationToken);

        await _audit.LogAsync(new AuditEvent
        {
            Action = "BedTransfer.Request",
            EntityType = "BedTransfer",
            EntityId = transfer.Id.ToString(),
            ActorUserId = _currentUser.UserId,
            Outcome = $"Raised bed transfer request for {transfer.PatientName} from {transfer.FromUnit} to {transfer.ToUnit} (Priority: {transfer.Priority})"
        }, cancellationToken);

        return Ok(transfer);
    }

    /// <summary>
    /// Assign bed with concurrent assignment guard preventing double-allocation (FR-BED-005).
    /// </summary>
    [HttpPut("transfers/{id:guid}/assign")]
    [Authorize(Policy = AppPermissions.BedsManage)]
    public async Task<IActionResult> AssignBed(Guid id, [FromBody] AssignBedRequest request, CancellationToken cancellationToken)
    {
        var assignedBy = _currentUser.UserName ?? "Ward Coordinator";

        try
        {
            var transfer = await _beds.AssignBedAsync(id, request.BedId, assignedBy, cancellationToken);

            await _audit.LogAsync(new AuditEvent
            {
                Action = "BedTransfer.AssignBed",
                EntityType = "BedTransfer",
                EntityId = transfer.Id.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = $"Assigned bed to transfer request for {transfer.PatientName}. Assigned Bed: {transfer.AssignedBedNumber}"
            }, cancellationToken);

            return Ok(transfer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Transition transfer stage: Requested -> BedAssigned -> Dispatched -> Received -> Completed (FR-BED-003).
    /// </summary>
    [HttpPut("transfers/{id:guid}/stage")]
    [Authorize(Policy = AppPermissions.BedsManage)]
    public async Task<IActionResult> UpdateTransferStage(Guid id, [FromBody] UpdateBedStageRequest request, CancellationToken cancellationToken)
    {
        var updatedBy = _currentUser.UserName ?? "Nurse";

        try
        {
            var transfer = await _beds.UpdateTransferStageAsync(id, request.Stage, updatedBy, cancellationToken);
            return Ok(transfer);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update bed housekeeping status (e.g. Cleaning -> Available) (FR-BED-001).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = AppPermissions.BedsManage)]
    public async Task<IActionResult> UpdateBedStatus(Guid id, [FromQuery] string status, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(status))
            return BadRequest(new { message = "Status is required." });

        try
        {
            var bed = await _beds.UpdateBedStatusAsync(id, status, cancellationToken);
            return Ok(bed);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Turnaround time analytics (average, median, 90th percentile by unit) (FR-BED-007).
    /// </summary>
    [HttpGet("turnaround-report")]
    [Authorize(Policy = AppPermissions.BedsRead)]
    public async Task<IActionResult> GetTurnaroundReport(CancellationToken cancellationToken)
    {
        var report = await _beds.GetTurnaroundReportAsync(cancellationToken);
        return Ok(report);
    }
}
