using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Domain.Constants;
using SmartCare.Infrastructure.Data;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OutboxController : ControllerBase
{
    private readonly ISqlExecutor _sql;

    public OutboxController(ISqlExecutor sql)
    {
        _sql = sql;
    }

    /// <summary>
    /// View queued/stubbed SMS and email outbox notifications (Excel Sprint Plan stub audit).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AppPermissions.OutboxRead)]
    public async Task<IActionResult> ListOutbox(CancellationToken cancellationToken)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_ListOutbox",
            r => new
            {
                OutboxId = r.Get<long>("OutboxId"),
                Channel = r.Get<string>("Channel"),
                Recipient = r.Get<string>("Recipient"),
                Subject = r.Get<string>("Subject"),
                Body = r.Get<string>("Body"),
                Status = r.Get<string>("Status"),
                CreatedAtUtc = r.Get<DateTimeOffset>("CreatedAtUtc")
            },
            cancellationToken: cancellationToken);

        return Ok(rows);
    }
}
