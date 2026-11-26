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
[Authorize(Policy = AppPermissions.AppointmentsRead)]
public sealed class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointments;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<BookAppointmentRequest> _bookValidator;
    private readonly IValidator<RescheduleAppointmentRequest> _rescheduleValidator;
    private readonly IValidator<CancelAppointmentRequest> _cancelValidator;
    private readonly IValidator<CheckInRequest> _checkInValidator;

    public AppointmentsController(
        IAppointmentService appointments,
        ICurrentUser currentUser,
        IValidator<BookAppointmentRequest> bookValidator,
        IValidator<RescheduleAppointmentRequest> rescheduleValidator,
        IValidator<CancelAppointmentRequest> cancelValidator,
        IValidator<CheckInRequest> checkInValidator)
    {
        _appointments = appointments;
        _currentUser = currentUser;
        _bookValidator = bookValidator;
        _rescheduleValidator = rescheduleValidator;
        _cancelValidator = cancelValidator;
        _checkInValidator = checkInValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        Guid? doctorId = _currentUser.HasPermission(AppPermissions.AppointmentsRead) && !_currentUser.HasPermission(AppPermissions.AppointmentsBook)
            ? ParseUserId()
            : null;

        var list = await _appointments.ListAsync(doctorId, null, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _appointments.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("patients")]
    [Authorize(Policy = AppPermissions.AppointmentsPatientsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PatientListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Patients(CancellationToken cancellationToken) =>
        Ok(await _appointments.ListPatientsAsync(cancellationToken));

    [HttpGet("doctors")]
    [Authorize(Policy = AppPermissions.AppointmentsDoctorsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Doctors(CancellationToken cancellationToken) =>
        Ok(await _appointments.ListDoctorsAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = AppPermissions.AppointmentsBook)]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book([FromBody] BookAppointmentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _bookValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var booked = await _appointments.BookAsync(
                request.PatientId,
                request.DoctorUserId,
                request.Department.Trim(),
                request.ScheduledAtUtc,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = booked.Id }, booked);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiMessageResponse { Message = ex.Message });
        }
    }

    [HttpPut("{id:int}/reschedule")]
    [Authorize(Policy = AppPermissions.AppointmentsReschedule)]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reschedule(
        int id,
        [FromBody] RescheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _rescheduleValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        if (await _appointments.GetByIdAsync(id, cancellationToken) is null)
            return NotFound();

        try
        {
            await _appointments.RescheduleAsync(id, request.ScheduledAtUtc, cancellationToken);
            return Ok(await _appointments.GetByIdAsync(id, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiMessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = AppPermissions.AppointmentsCancel)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] CancelAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        if (await _appointments.GetByIdAsync(id, cancellationToken) is null)
            return NotFound();

        await _appointments.CancelAsync(id, request.Reason.Trim(), cancellationToken);
        return Ok(new ApiMessageResponse { Message = "Appointment cancelled. The slot is free." });
    }

    [HttpPost("check-in")]
    [Authorize(Policy = AppPermissions.AppointmentsCheckIn)]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request, CancellationToken cancellationToken)
    {
        var validation = await _checkInValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        var item = await _appointments.CheckInAsync(request.Code, cancellationToken);
        if (item is null)
            return NotFound(new ApiMessageResponse { Message = "Code not found, already used, or expired booking." });

        return Ok(item);
    }

    private Guid? ParseUserId() =>
        Guid.TryParse(_currentUser.UserId, out var id) ? id : null;
}
