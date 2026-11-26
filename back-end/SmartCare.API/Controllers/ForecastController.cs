using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ForecastController : ControllerBase
{
    private readonly IForecastService _forecast;

    public ForecastController(IForecastService forecast) => _forecast = forecast;

    [HttpGet("occupancy")]
    [Authorize(Policy = AppPermissions.ForecastRead)]
    public async Task<IActionResult> Occupancy([FromQuery] int days = 7, CancellationToken ct = default)
    {
        if (days < 1) days = 1;
        if (days > 30) days = 30;
        return Ok(await _forecast.GetOccupancyAsync(days, ct));
    }
}
