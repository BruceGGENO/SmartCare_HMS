using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.DTOs;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPermissions.RevenueRead)]
public sealed class RevenueController : ControllerBase
{
    private readonly IRevenueAccountingService _revenueService;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;

    public RevenueController(
        IRevenueAccountingService revenueService,
        ICurrentUser currentUser,
        IAuditLogger audit)
    {
        _revenueService = revenueService;
        _currentUser = currentUser;
        _audit = audit;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(RevenueSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _revenueService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("packages")]
    [ProducesResponseType(typeof(IReadOnlyList<PackageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPackages(CancellationToken cancellationToken)
    {
        var packages = await _revenueService.GetAllPackagesAsync(cancellationToken);
        return Ok(packages);
    }

    [HttpGet("usages")]
    [ProducesResponseType(typeof(IReadOnlyList<PackageUsageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsages(CancellationToken cancellationToken)
    {
        var usages = await _revenueService.GetRecentUsagesAsync(cancellationToken);
        return Ok(usages);
    }

    [HttpPost("packages/purchase")]
    [Authorize(Policy = AppPermissions.RevenueManage)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PurchasePackage(
        [FromBody] PurchasePackageDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.TotalSessions <= 0 || dto.TotalAmount <= 0)
            return BadRequest(new ApiMessageResponse { Message = "Sessions and Amount must be greater than zero." });

        var success = await _revenueService.PurchasePackageAsync(dto, cancellationToken);
        if (!success)
            return BadRequest(new ApiMessageResponse { Message = "Failed to record package purchase." });

        await _audit.LogAsync(new AuditEvent
        {
            Action = "Revenue.PackagePurchase",
            EntityType = "Package",
            EntityId = dto.PackageName,
            ActorUserId = _currentUser.UserId,
            Outcome = "Success"
        }, cancellationToken);

        return CreatedAtAction(nameof(GetPackages), new ApiMessageResponse { Message = "Package purchased and recorded." });
    }

    [HttpPost("packages/{id:int}/consume")]
    [Authorize(Policy = AppPermissions.RevenueManage)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConsumeSession(
        int id,
        [FromBody] ConsumeSessionRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _revenueService.ConsumeSessionAsync(id, request.Notes, cancellationToken);
        if (!success)
            return BadRequest(new ApiMessageResponse { Message = "No remaining sessions or package not found." });

        await _audit.LogAsync(new AuditEvent
        {
            Action = "Revenue.SessionConsumed",
            EntityType = "Package",
            EntityId = id.ToString(),
            ActorUserId = _currentUser.UserId,
            Outcome = "Success"
        }, cancellationToken);

        return Ok(new ApiMessageResponse { Message = "Session consumed and revenue recognized." });
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken)
    {
        var bytes = await _revenueService.ExportReconciliationCsvAsync(cancellationToken);
        return File(bytes, "text/csv", $"Revenue_Reconciliation_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }
}

public sealed class ConsumeSessionRequest
{
    public string? Notes { get; set; }
}
