using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OccupancyController : ControllerBase
{
    private readonly IOccupancyForecastingService _forecaster;

    public OccupancyController(IOccupancyForecastingService forecaster)
    {
        _forecaster = forecaster;
    }

    /// <summary>
    /// Historical 90-day bed occupancy snapshots per ward (FR-OCC-001).
    /// </summary>
    [HttpGet("snapshots")]
    [Authorize(Policy = AppPermissions.OccupancyRead)]
    public async Task<IActionResult> GetHistoricalSnapshots([FromQuery] string? ward, [FromQuery] int days = 90, CancellationToken cancellationToken = default)
    {
        var snapshots = await _forecaster.GetHistoricalSnapshotsAsync(ward, days, cancellationToken);
        return Ok(snapshots);
    }

    /// <summary>
    /// Trigger the daily occupancy snapshot job (FR-OCC-001).
    /// </summary>
    [HttpPost("snapshots/run")]
    [Authorize(Policy = AppPermissions.OccupancyRead)]
    public async Task<IActionResult> RunDailySnapshot(CancellationToken cancellationToken)
    {
        var snapshot = await _forecaster.TriggerDailySnapshotAsync(cancellationToken);
        return Ok(snapshot);
    }

    /// <summary>
    /// 7-14 day predictive bed occupancy forecast with confidence intervals (FR-OCC-002, FR-OCC-003).
    /// </summary>
    [HttpGet("forecast")]
    [Authorize(Policy = AppPermissions.OccupancyRead)]
    public async Task<IActionResult> GetForecast([FromQuery] string? ward, [FromQuery] int horizonDays = 14, CancellationToken cancellationToken = default)
    {
        var forecast = await _forecaster.GetForecastAsync(ward, horizonDays, cancellationToken);
        return Ok(forecast);
    }

    /// <summary>
    /// Active surge alerts where forecasted occupancy exceeds 85% threshold (FR-OCC-004).
    /// </summary>
    [HttpGet("surge-alerts")]
    [Authorize(Policy = AppPermissions.OccupancyRead)]
    public async Task<IActionResult> GetSurgeAlerts(CancellationToken cancellationToken)
    {
        var alerts = await _forecaster.GetSurgeAlertsAsync(cancellationToken);
        return Ok(alerts);
    }
}
