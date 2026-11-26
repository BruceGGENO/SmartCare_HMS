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
public sealed class BillingController : ControllerBase
{
    private readonly IBillingService _billing;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;
    private readonly IValidator<CreateInvoiceRequest> _createValidator;
    private readonly IValidator<PayInvoiceRequest> _payValidator;

    public BillingController(
        IBillingService billing,
        ICurrentUser currentUser,
        IAuditLogger audit,
        IValidator<CreateInvoiceRequest> createValidator,
        IValidator<PayInvoiceRequest> payValidator)
    {
        _billing = billing;
        _currentUser = currentUser;
        _audit = audit;
        _createValidator = createValidator;
        _payValidator = payValidator;
    }

    [HttpGet("invoices/pending")]
    [Authorize(Policy = AppPermissions.BillingRead)]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPending(CancellationToken cancellationToken) =>
        Ok(await _billing.ListPendingAsync(cancellationToken));

    [HttpGet("invoices")]
    [Authorize(Policy = AppPermissions.BillingRead)]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int? patientId,
        [FromQuery] string? status,
        CancellationToken cancellationToken) =>
        Ok(await _billing.ListAsync(patientId, status, cancellationToken));

    [HttpGet("invoices/{id:int}")]
    [Authorize(Policy = AppPermissions.BillingRead)]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var invoice = await _billing.GetByIdAsync(id, cancellationToken);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpPost("invoices")]
    [Authorize(Policy = AppPermissions.BillingCreate)]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var created = await _billing.CreateAsync(request.PatientId, request.Lines, cancellationToken);

            await _audit.LogAsync(new AuditEvent
            {
                Action = "Invoice.Create",
                EntityType = "Invoice",
                EntityId = created.InvoiceId.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.InvoiceId }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiMessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("invoices/{id:int}/pay")]
    [Authorize(Policy = AppPermissions.BillingPay)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pay(
        int id,
        [FromBody] PayInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _payValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        if (await _billing.GetByIdAsync(id, cancellationToken) is null)
            return NotFound();

        try
        {
            var payment = await _billing.PayAsync(
                id,
                request.Amount,
                request.PaymentMethod,
                request.PhoneNumber,
                cancellationToken);

            await _audit.LogAsync(new AuditEvent
            {
                Action = "Invoice.Pay",
                EntityType = "Invoice",
                EntityId = id.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, cancellationToken);

            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiMessageResponse { Message = ex.Message });
        }
    }
}
