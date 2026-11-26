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
public sealed class LabController : ControllerBase
{
    private readonly ILabService _lab;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;
    private readonly IValidator<CreateLabOrderRequest> _createValidator;
    private readonly IValidator<FileLabResultRequest> _resultValidator;

    public LabController(
        ILabService lab, ICurrentUser currentUser, IAuditLogger audit,
        IValidator<CreateLabOrderRequest> createValidator,
        IValidator<FileLabResultRequest> resultValidator)
    {
        _lab = lab;
        _currentUser = currentUser;
        _audit = audit;
        _createValidator = createValidator;
        _resultValidator = resultValidator;
    }

    [HttpGet("tests")]
    [Authorize(Policy = AppPermissions.LabRead)]
    public async Task<IActionResult> Tests(CancellationToken ct) => Ok(await _lab.ListTestsAsync(ct));

    [HttpGet("orders")]
    [Authorize(Policy = AppPermissions.LabRead)]
    public async Task<IActionResult> Orders([FromQuery] int? patientId, [FromQuery] string? status, CancellationToken ct) =>
        Ok(await _lab.ListOrdersAsync(patientId, status, ct));

    [HttpGet("orders/{id:int}")]
    [Authorize(Policy = AppPermissions.LabRead)]
    public async Task<IActionResult> GetOrder(int id, CancellationToken ct)
    {
        var item = await _lab.GetOrderAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("orders")]
    [Authorize(Policy = AppPermissions.LabOrder)]
    public async Task<IActionResult> Create([FromBody] CreateLabOrderRequest request, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        Guid? actor = Guid.TryParse(_currentUser.UserId, out var id) ? id : null;
        try
        {
            var created = await _lab.CreateOrderAsync(request.PatientId, request.LabTestId, actor, ct);
            await _audit.LogAsync(new AuditEvent
            {
                Action = "Lab.Order",
                EntityType = "LabOrder",
                EntityId = created.LabOrderId.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, ct);
            return CreatedAtAction(nameof(GetOrder), new { id = created.LabOrderId }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiMessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("orders/{id:int}/result")]
    [Authorize(Policy = AppPermissions.LabResult)]
    public async Task<IActionResult> FileResult(int id, [FromBody] FileLabResultRequest request, CancellationToken ct)
    {
        var validation = await _resultValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        if (await _lab.GetOrderAsync(id, ct) is null)
            return NotFound();

        Guid? actor = Guid.TryParse(_currentUser.UserId, out var uid) ? uid : null;
        try
        {
            var updated = await _lab.FileResultAsync(id, request.ResultValue, request.ResultNotes, actor, ct);
            await _audit.LogAsync(new AuditEvent
            {
                Action = "Lab.Result",
                EntityType = "LabOrder",
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
