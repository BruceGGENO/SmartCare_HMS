using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PortalController : ControllerBase
{
    private readonly IAppointmentService _appointments;
    private readonly ICurrentUser _currentUser;

    public PortalController(IAppointmentService appointments, ICurrentUser currentUser)
    {
        _appointments = appointments;
        _currentUser = currentUser;
    }

    [HttpGet("appointments")]
    [Authorize(Policy = AppPermissions.PortalAppointmentsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MyAppointments(CancellationToken cancellationToken)
    {
        var patientId = await RequirePatientId(cancellationToken);
        if (patientId is null)
            return NotFound(new ApiMessageResponse { Message = "No patient record is linked to this account." });

        return Ok(await _appointments.ListAsync(null, patientId, cancellationToken));
    }

    [HttpGet("appointments/{id:int}")]
    [Authorize(Policy = AppPermissions.PortalAppointmentsRead)]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AppointmentDetails(int id, CancellationToken cancellationToken)
    {
        var patientId = await RequirePatientId(cancellationToken);
        var item = await _appointments.GetByIdAsync(id, cancellationToken);
        if (patientId is null || item is null)
            return NotFound();
        if (item.PatientId != patientId)
            return Forbid();

        return Ok(item);
    }

    [HttpGet("history")]
    [Authorize(Policy = AppPermissions.PortalHistoryRead)]
    [ProducesResponseType(typeof(PortalHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var patientId = await RequirePatientId(cancellationToken);
        if (patientId is null)
            return NotFound(new ApiMessageResponse { Message = "No patient record is linked to this account." });

        var history = await _appointments.ListHistoryAsync(patientId.Value, cancellationToken);
        var notes = await _appointments.ListReleasedNotesAsync(patientId.Value, cancellationToken);
        return Ok(new PortalHistoryResponse { Appointments = history, Notes = notes });
    }

    [HttpGet("packages")]
    [Authorize(Policy = AppPermissions.PortalPackagesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PackageBalanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Packages(CancellationToken cancellationToken)
    {
        var patientId = await RequirePatientId(cancellationToken);
        if (patientId is null)
            return NotFound(new ApiMessageResponse { Message = "No patient record is linked to this account." });

        return Ok(await _appointments.ListPackagesAsync(patientId.Value, cancellationToken));
    }

    private async Task<int?> RequirePatientId(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUser.UserId, out var userId))
            return null;
        return await _appointments.FindPatientIdByUserIdAsync(userId, cancellationToken);
    }
}
