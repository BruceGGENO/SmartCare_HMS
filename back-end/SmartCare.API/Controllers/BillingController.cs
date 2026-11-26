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
[Authorize(Policy = AppPermissions.BillingRead)]
public sealed class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;

    public BillingController(
        IBillingService billingService,
        ICurrentUser currentUser,
        IAuditLogger audit)
    {
        _billingService = billingService;
        _currentUser = currentUser;
        _audit = audit;
    }

    [HttpGet("invoices/pending")]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingInvoices(CancellationToken cancellationToken)
    {
        var invoices = await _billingService.GetPendingInvoicesAsync(cancellationToken);
        return Ok(invoices);
    }

    [HttpGet("invoices/{id:int}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoiceById(int id, CancellationToken cancellationToken)
    {
        var invoice = await _billingService.GetInvoiceByIdAsync(id, cancellationToken);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpPost("invoices/{id:int}/pay")]
    [Authorize(Policy = AppPermissions.BillingPay)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PayInvoice(
        int id,
        [FromBody] PaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        var invoice = await _billingService.GetInvoiceByIdAsync(id, cancellationToken);
        if (invoice is null) return NotFound();

        var success = await _billingService.PayInvoiceAsync(
            id,
            request.Amount,
            request.PaymentMethod,
            request.PhoneNumber ?? "",
            cancellationToken);

        if (!success)
        {
            return BadRequest(new ApiMessageResponse { Message = "Payment transaction failed or declined by gateway." });
        }

        await _audit.LogAsync(new AuditEvent
        {
            Action = "Billing.Payment",
            EntityType = "Invoice",
            EntityId = id.ToString(),
            ActorUserId = _currentUser.UserId,
            Outcome = "Success"
        }, cancellationToken);

        return Ok(new ApiMessageResponse { Message = "Payment processed successfully." });
    }
}

public sealed class PaymentRequestDto
{
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "MobileMoney";
    public string? PhoneNumber { get; set; }
}
