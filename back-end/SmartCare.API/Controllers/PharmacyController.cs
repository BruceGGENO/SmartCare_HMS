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
public sealed class PharmacyController : ControllerBase
{
    private readonly IPharmacyService _pharmacy;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;
    private readonly IValidator<CreatePrescriptionRequest> _prescribeValidator;

    public PharmacyController(
        IPharmacyService pharmacy, ICurrentUser currentUser, IAuditLogger audit,
        IValidator<CreatePrescriptionRequest> prescribeValidator)
    {
        _pharmacy = pharmacy;
        _currentUser = currentUser;
        _audit = audit;
        _prescribeValidator = prescribeValidator;
    }

    [HttpGet("drugs")]
    [Authorize(Policy = AppPermissions.PharmacyRead)]
    public async Task<IActionResult> Drugs(CancellationToken ct) => Ok(await _pharmacy.ListDrugsAsync(ct));

    [HttpGet("prescriptions")]
    [Authorize(Policy = AppPermissions.PharmacyRead)]
    public async Task<IActionResult> Prescriptions([FromQuery] int? patientId, [FromQuery] string? status, CancellationToken ct) =>
        Ok(await _pharmacy.ListPrescriptionsAsync(patientId, status, ct));

    [HttpGet("prescriptions/{id:int}")]
    [Authorize(Policy = AppPermissions.PharmacyRead)]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var item = await _pharmacy.GetPrescriptionAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("prescriptions")]
    [Authorize(Policy = AppPermissions.PharmacyPrescribe)]
    public async Task<IActionResult> Prescribe([FromBody] CreatePrescriptionRequest request, CancellationToken ct)
    {
        var validation = await _prescribeValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        Guid? actor = Guid.TryParse(_currentUser.UserId, out var id) ? id : null;
        try
        {
            var created = await _pharmacy.PrescribeAsync(request.PatientId, request.DrugId, request.Dose, request.Quantity, actor, ct);
            await _audit.LogAsync(new AuditEvent
            {
                Action = "Pharmacy.Prescribe",
                EntityType = "Prescription",
                EntityId = created.PrescriptionId.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, ct);
            return CreatedAtAction(nameof(Get), new { id = created.PrescriptionId }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiMessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("prescriptions/{id:int}/dispense")]
    [Authorize(Policy = AppPermissions.PharmacyDispense)]
    public async Task<IActionResult> Dispense(int id, CancellationToken ct)
    {
        if (await _pharmacy.GetPrescriptionAsync(id, ct) is null)
            return NotFound();

        Guid? actor = Guid.TryParse(_currentUser.UserId, out var uid) ? uid : null;
        try
        {
            var updated = await _pharmacy.DispenseAsync(id, actor, ct);
            await _audit.LogAsync(new AuditEvent
            {
                Action = "Pharmacy.Dispense",
                EntityType = "Prescription",
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
