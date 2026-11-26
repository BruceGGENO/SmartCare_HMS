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
public sealed class RevenueController : ControllerBase
{
    private readonly IRevenueService _revenue;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;
    private readonly IValidator<PurchasePackageRequest> _purchaseValidator;
    private readonly IValidator<ConsumePackageSessionRequest> _consumeValidator;

    public RevenueController(
        IRevenueService revenue,
        ICurrentUser currentUser,
        IAuditLogger audit,
        IValidator<PurchasePackageRequest> purchaseValidator,
        IValidator<ConsumePackageSessionRequest> consumeValidator)
    {
        _revenue = revenue;
        _currentUser = currentUser;
        _audit = audit;
        _purchaseValidator = purchaseValidator;
        _consumeValidator = consumeValidator;
    }

    [HttpGet("summary")]
    [Authorize(Policy = AppPermissions.RevenueRead)]
    [ProducesResponseType(typeof(RevenueSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken) =>
        Ok(await _revenue.GetSummaryAsync(cancellationToken));

    [HttpGet("packages")]
    [Authorize(Policy = AppPermissions.RevenueRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PrepaidPackageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPackages(
        [FromQuery] int? patientId,
        CancellationToken cancellationToken) =>
        Ok(await _revenue.ListPackagesAsync(patientId, cancellationToken));

    [HttpGet("packages/{id:int}")]
    [Authorize(Policy = AppPermissions.RevenueRead)]
    [ProducesResponseType(typeof(PrepaidPackageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPackage(int id, CancellationToken cancellationToken)
    {
        var package = await _revenue.GetPackageByIdAsync(id, cancellationToken);
        return package is null ? NotFound() : Ok(package);
    }

    [HttpGet("usages")]
    [Authorize(Policy = AppPermissions.RevenueRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PackageUsageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecentUsages(CancellationToken cancellationToken) =>
        Ok(await _revenue.ListRecentUsagesAsync(cancellationToken));

    [HttpGet("export")]
    [Authorize(Policy = AppPermissions.RevenueRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken)
    {
        var bytes = await _revenue.ExportReconciliationCsvAsync(cancellationToken);
        var fileName = $"revenue-reconciliation-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
        return File(bytes, "text/csv", fileName);
    }

    [HttpPost("packages")]
    [Authorize(Policy = AppPermissions.RevenuePurchase)]
    [ProducesResponseType(typeof(PrepaidPackageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Purchase(
        [FromBody] PurchasePackageRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _purchaseValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var created = await _revenue.PurchaseAsync(
                request.PatientId,
                request.PackageName,
                request.TotalSessions,
                request.TotalAmount,
                cancellationToken);

            await _audit.LogAsync(new AuditEvent
            {
                Action = "Package.Purchase",
                EntityType = "PrepaidPackage",
                EntityId = created.PackageId.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, cancellationToken);

            return CreatedAtAction(nameof(GetPackage), new { id = created.PackageId }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiMessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("packages/{id:int}/consume")]
    [Authorize(Policy = AppPermissions.RevenueConsume)]
    [ProducesResponseType(typeof(PackageUsageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Consume(
        int id,
        [FromBody] ConsumePackageSessionRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ConsumePackageSessionRequest();
        var validation = await _consumeValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        if (await _revenue.GetPackageByIdAsync(id, cancellationToken) is null)
            return NotFound();

        try
        {
            var usage = await _revenue.ConsumeSessionAsync(id, request.Notes, cancellationToken);

            await _audit.LogAsync(new AuditEvent
            {
                Action = "Package.Consume",
                EntityType = "PrepaidPackage",
                EntityId = id.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, cancellationToken);

            return Ok(usage);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiMessageResponse { Message = ex.Message });
        }
    }
}
