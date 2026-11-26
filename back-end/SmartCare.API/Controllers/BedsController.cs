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
public sealed class BedsController : ControllerBase
{
    private readonly IBedService _beds;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;
    private readonly IValidator<AdmitPatientRequest> _admitValidator;
    private readonly IValidator<TransferAdmissionRequest> _transferValidator;

    public BedsController(
        IBedService beds, ICurrentUser currentUser, IAuditLogger audit,
        IValidator<AdmitPatientRequest> admitValidator,
        IValidator<TransferAdmissionRequest> transferValidator)
    {
        _beds = beds;
        _currentUser = currentUser;
        _audit = audit;
        _admitValidator = admitValidator;
        _transferValidator = transferValidator;
    }

    [HttpGet("wards")]
    [Authorize(Policy = AppPermissions.BedsRead)]
    public async Task<IActionResult> Wards(CancellationToken ct) => Ok(await _beds.ListWardsAsync(ct));

    [HttpGet]
    [Authorize(Policy = AppPermissions.BedsRead)]
    public async Task<IActionResult> List([FromQuery] int? wardId, CancellationToken ct) =>
        Ok(await _beds.ListBedsAsync(wardId, ct));

    [HttpGet("admissions")]
    [Authorize(Policy = AppPermissions.BedsRead)]
    public async Task<IActionResult> Admissions([FromQuery] string? status, CancellationToken ct) =>
        Ok(await _beds.ListAdmissionsAsync(status, ct));

    [HttpGet("admissions/{id:int}")]
    [Authorize(Policy = AppPermissions.BedsRead)]
    public async Task<IActionResult> GetAdmission(int id, CancellationToken ct)
    {
        var item = await _beds.GetAdmissionAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("admit")]
    [Authorize(Policy = AppPermissions.BedsAssign)]
    public async Task<IActionResult> Admit([FromBody] AdmitPatientRequest request, CancellationToken ct)
    {
        var validation = await _admitValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var created = await _beds.AdmitAsync(request.PatientId, request.BedId, ct);
            await _audit.LogAsync(new AuditEvent
            {
                Action = "Bed.Admit",
                EntityType = "Admission",
                EntityId = created.AdmissionId.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, ct);
            return CreatedAtAction(nameof(GetAdmission), new { id = created.AdmissionId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiMessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("admissions/{id:int}/transfer")]
    [Authorize(Policy = AppPermissions.BedsAssign)]
    public async Task<IActionResult> Transfer(int id, [FromBody] TransferAdmissionRequest request, CancellationToken ct)
    {
        var validation = await _transferValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        if (await _beds.GetAdmissionAsync(id, ct) is null)
            return NotFound();

        try
        {
            var updated = await _beds.TransferAsync(id, request.NewBedId, ct);
            await _audit.LogAsync(new AuditEvent
            {
                Action = "Bed.Transfer",
                EntityType = "Admission",
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

    [HttpPost("admissions/{id:int}/free")]
    [Authorize(Policy = AppPermissions.BedsAssign)]
    public async Task<IActionResult> Free(int id, CancellationToken ct)
    {
        if (await _beds.GetAdmissionAsync(id, ct) is null)
            return NotFound();

        try
        {
            var updated = await _beds.FreeBedAsync(id, ct);
            await _audit.LogAsync(new AuditEvent
            {
                Action = "Bed.Free",
                EntityType = "Admission",
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
