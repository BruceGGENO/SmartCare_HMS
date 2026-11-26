using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LabController : ControllerBase
{
    private readonly ILabService _lab;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;

    public LabController(
        ILabService lab,
        ICurrentUser currentUser,
        IAuditLogger audit)
    {
        _lab = lab;
        _currentUser = currentUser;
        _audit = audit;
    }

    /// <summary>
    /// Test and radiology catalog with reference ranges and turnaround times (FR-LAB-001).
    /// </summary>
    [HttpGet("catalog")]
    [Authorize(Policy = AppPermissions.LabRead)]
    public async Task<IActionResult> GetCatalog([FromQuery] string? category, CancellationToken cancellationToken)
    {
        var catalog = await _lab.GetCatalogAsync(category, cancellationToken);
        return Ok(catalog);
    }

    /// <summary>
    /// Active worklist of lab and radiology orders (FR-LAB-001, FR-LAB-002).
    /// </summary>
    [HttpGet("orders")]
    [Authorize(Policy = AppPermissions.LabRead)]
    public async Task<IActionResult> GetOrders([FromQuery] string? status, [FromQuery] Guid? patientId, CancellationToken cancellationToken)
    {
        var orders = await _lab.GetOrdersAsync(status, patientId, cancellationToken);
        return Ok(orders);
    }

    /// <summary>
    /// Doctor orders a lab or radiology test (FR-LAB-001).
    /// </summary>
    [HttpPost("orders")]
    [Authorize(Policy = AppPermissions.LabManage)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateLabOrderRequest request, CancellationToken cancellationToken)
    {
        var orderedBy = _currentUser.UserName ?? "Doctor";
        var order = await _lab.CreateOrderAsync(request, orderedBy, cancellationToken);

        await _audit.LogAsync(new AuditEvent
        {
            Action = "Lab.OrderTest",
            EntityType = "LabOrder",
            EntityId = order.Id.ToString(),
            ActorUserId = _currentUser.UserId,
            Outcome = $"Ordered {order.TestName} ({order.Category}) for patient {order.PatientName}. Sample ID: {order.SampleId}"
        }, cancellationToken);

        return Ok(order);
    }

    /// <summary>
    /// Track sample status: Collected -> Received -> In-process -> Completed (FR-LAB-002).
    /// </summary>
    [HttpPut("orders/{id:guid}/sample-status")]
    [Authorize(Policy = AppPermissions.LabManage)]
    public async Task<IActionResult> UpdateSampleStatus(Guid id, [FromBody] UpdateSampleStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _lab.UpdateSampleStatusAsync(id, request, cancellationToken);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Enter structured result with reference ranges and abnormal/critical flags (FR-LAB-003, FR-LAB-007).
    /// </summary>
    [HttpPost("orders/{id:guid}/result")]
    [Authorize(Policy = AppPermissions.LabManage)]
    public async Task<IActionResult> SubmitResult(Guid id, [FromBody] SubmitLabResultRequest request, CancellationToken cancellationToken)
    {
        var tech = _currentUser.UserName ?? "Lab Technician";

        try
        {
            var order = await _lab.SubmitResultAsync(id, request, tech, cancellationToken);

            await _audit.LogAsync(new AuditEvent
            {
                Action = "Lab.SubmitResult",
                EntityType = "LabOrder",
                EntityId = order.Id.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = $"Submitted lab result for {order.TestName}. Value: {order.ResultValue} (Critical: {order.IsCritical})"
            }, cancellationToken);

            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Senior technician or pathologist verifies and releases result to EHR and patient portal (FR-LAB-005).
    /// </summary>
    [HttpPost("orders/{id:guid}/verify")]
    [Authorize(Policy = AppPermissions.LabManage)]
    public async Task<IActionResult> VerifyResult(Guid id, CancellationToken cancellationToken)
    {
        var verifier = _currentUser.UserName ?? "Lab Senior";

        try
        {
            var order = await _lab.VerifyResultAsync(id, verifier, cancellationToken);

            await _audit.LogAsync(new AuditEvent
            {
                Action = "Lab.VerifyResult",
                EntityType = "LabOrder",
                EntityId = order.Id.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = $"Verified and released lab result for {order.TestName} (Order: {order.OrderNumber}) to EHR and portal."
            }, cancellationToken);

            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
