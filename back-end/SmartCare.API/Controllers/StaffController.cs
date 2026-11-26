using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StaffController : ControllerBase
{
    private readonly IStaffService _staff;

    public StaffController(IStaffService staff)
    {
        _staff = staff;
    }

    /// <summary>
    /// List staff profiles, roles, departments and license status (FR-STF-001).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AppPermissions.StaffRead)]
    public async Task<IActionResult> GetStaffProfiles([FromQuery] string? department, CancellationToken cancellationToken)
    {
        var profiles = await _staff.GetStaffProfilesAsync(department, cancellationToken);
        return Ok(profiles);
    }

    /// <summary>
    /// View duty rosters and shift assignments by department (FR-STF-002, FR-STF-003).
    /// </summary>
    [HttpGet("rosters")]
    [Authorize(Policy = AppPermissions.StaffRead)]
    public async Task<IActionResult> GetRosters([FromQuery] DateTime? period, CancellationToken cancellationToken)
    {
        var rosters = await _staff.GetRostersAsync(period, cancellationToken);
        return Ok(rosters);
    }

    /// <summary>
    /// HR/Payroll hook: Export staff attendance and shift hours as CSV (FR-STF-007).
    /// </summary>
    [HttpGet("attendance/export")]
    [Authorize(Policy = AppPermissions.StaffRead)]
    public async Task<IActionResult> ExportAttendance(
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        CancellationToken cancellationToken)
    {
        var startDate = start ?? DateTime.UtcNow.AddDays(-7);
        var endDate = end ?? DateTime.UtcNow;

        var csvBytes = await _staff.ExportAttendanceCsvAsync(startDate, endDate, cancellationToken);
        return File(csvBytes, "text/csv", $"staff_attendance_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv");
    }
}
