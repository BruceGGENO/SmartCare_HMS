using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PharmacyController : ControllerBase
{
    private readonly IPharmacyService _pharmacy;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;

    public PharmacyController(
        IPharmacyService pharmacy,
        ICurrentUser currentUser,
        IAuditLogger audit)
    {
        _pharmacy = pharmacy;
        _currentUser = currentUser;
        _audit = audit;
    }

    /// <summary>
    /// Drug catalog with dosage, unit prices and total stock levels (FR-PHM-001, FR-PHM-002).
    /// </summary>
    [HttpGet("catalog")]
    [Authorize(Policy = AppPermissions.PharmacyRead)]
    public async Task<IActionResult> GetCatalog([FromQuery] string? query, CancellationToken cancellationToken)
    {
        var catalog = await _pharmacy.GetDrugCatalogAsync(query, cancellationToken);
        return Ok(catalog);
    }

    /// <summary>
    /// Stock batches with batch numbers, supplier and expiry dates (FR-PHM-001, FR-PHM-006).
    /// </summary>
    [HttpGet("batches")]
    [Authorize(Policy = AppPermissions.PharmacyRead)]
    public async Task<IActionResult> GetBatches([FromQuery] Guid? drugId, CancellationToken cancellationToken)
    {
        var batches = await _pharmacy.GetStockBatchesAsync(drugId, cancellationToken);
        return Ok(batches);
    }

    /// <summary>
    /// Automated reorder alerts when stock falls below minimum threshold (FR-PHM-005).
    /// </summary>
    [HttpGet("alerts")]
    [Authorize(Policy = AppPermissions.PharmacyRead)]
    public async Task<IActionResult> GetReorderAlerts(CancellationToken cancellationToken)
    {
        var alerts = await _pharmacy.GetReorderAlertsAsync(cancellationToken);
        return Ok(alerts);
    }

    /// <summary>
    /// Dispense medication against prescription, check allergies, block negative stock, and post to invoice (FR-PHM-002, FR-PHM-003, FR-PHM-004, FR-PHM-008).
    /// </summary>
    [HttpPost("dispense")]
    [Authorize(Policy = AppPermissions.PharmacyDispense)]
    public async Task<IActionResult> Dispense([FromBody] DispenseMedicationRequest request, CancellationToken cancellationToken)
    {
        var dispenser = _currentUser.UserName ?? "Pharmacist";

        try
        {
            var result = await _pharmacy.DispenseMedicationAsync(request, dispenser, cancellationToken);

            await _audit.LogAsync(new AuditEvent
            {
                Action = "Pharmacy.Dispense",
                EntityType = "Medication",
                EntityId = result.Id.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = $"Dispensed {result.Quantity} units of {result.DrugName} (Batch: {result.BatchNumber}) for {result.PatientName}. Amount: {result.BilledAmount:C}"
            }, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
