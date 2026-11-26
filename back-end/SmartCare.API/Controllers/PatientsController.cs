using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PatientsController : ControllerBase
{
    private readonly IPatientEhrService _ehr;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;

    public PatientsController(
        IPatientEhrService ehr,
        ICurrentUser currentUser,
        IAuditLogger audit)
    {
        _ehr = ehr;
        _currentUser = currentUser;
        _audit = audit;
    }

    /// <summary>
    /// Search and list patients (demographics and MRN).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AppPermissions.PatientsRead)]
    public async Task<IActionResult> Search([FromQuery] string? query, CancellationToken cancellationToken)
    {
        var patients = await _ehr.SearchPatientsAsync(query, cancellationToken);
        return Ok(patients);
    }

    /// <summary>
    /// Get comprehensive EHR profile (demographics, allergies, diagnoses, treatment plan, history).
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AppPermissions.PatientsRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var profile = await _ehr.GetPatientByIdAsync(id, cancellationToken);
        if (profile is null)
            return NotFound(new { message = $"Patient with ID {id} not found." });

        // Audit access to PHI
        await _audit.LogAsync(new AuditEvent
        {
            Action = "EHR.Read",
            EntityType = "Patient",
            EntityId = id.ToString(),
            ActorUserId = _currentUser.UserId,
            Outcome = $"Viewed complete clinical EHR record for patient {profile.FullName} (MRN: {profile.Mrn})"
        }, cancellationToken);

        return Ok(profile);
    }

    /// <summary>
    /// Check for duplicate patient on matching Name + DOB + Phone before registration (FR-EHR-001).
    /// </summary>
    [HttpPost("check-duplicate")]
    [Authorize(Policy = AppPermissions.PatientsManage)]
    public async Task<IActionResult> CheckDuplicate([FromBody] RegisterPatientRequest request, CancellationToken cancellationToken)
    {
        var result = await _ehr.CheckDuplicateAsync(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.PhoneNumber,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Register a new patient and generate unique MRN (FR-EHR-001).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AppPermissions.PatientsManage)]
    public async Task<IActionResult> Register([FromBody] RegisterPatientRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { message = "First and last name are required." });

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return BadRequest(new { message = "Phone number is required." });

        var patient = await _ehr.RegisterPatientAsync(request, cancellationToken);

        await _audit.LogAsync(new AuditEvent
        {
            Action = "Patient.Register",
            EntityType = "Patient",
            EntityId = patient.Id.ToString(),
            ActorUserId = _currentUser.UserId,
            Outcome = $"Registered new patient {patient.FullName} with MRN {patient.Mrn}"
        }, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    /// <summary>
    /// Record an allergy with severity and reaction (FR-EHR-003).
    /// </summary>
    [HttpPost("{id:guid}/allergies")]
    [Authorize(Policy = AppPermissions.PatientsManage)]
    public async Task<IActionResult> AddAllergy(Guid id, [FromBody] AddAllergyRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Substance))
            return BadRequest(new { message = "Allergen substance is required." });

        try
        {
            var allergy = await _ehr.AddAllergyAsync(id, request, cancellationToken);
            return Ok(allergy);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Record an ICD-coded diagnosis linked to encounter (FR-EHR-004).
    /// </summary>
    [HttpPost("{id:guid}/diagnoses")]
    [Authorize(Policy = AppPermissions.PatientsManage)]
    public async Task<IActionResult> AddDiagnosis(Guid id, [FromBody] AddDiagnosisRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IcdCode) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { message = "ICD code and description are required." });

        var doctorName = _currentUser.UserName ?? "Dr. On Duty";

        try
        {
            var diagnosis = await _ehr.AddDiagnosisAsync(id, request, doctorName, cancellationToken);
            return Ok(diagnosis);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create or update clinical treatment plan (FR-EHR-005).
    /// </summary>
    [HttpPost("{id:guid}/treatment-plans")]
    [Authorize(Policy = AppPermissions.PatientsManage)]
    public async Task<IActionResult> SaveTreatmentPlan(Guid id, [FromBody] SaveTreatmentPlanRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Goals) && string.IsNullOrWhiteSpace(request.Medications))
            return BadRequest(new { message = "Treatment goals or medications are required." });

        var doctorName = _currentUser.UserName ?? "Dr. On Duty";

        try
        {
            var plan = await _ehr.SaveTreatmentPlanAsync(id, request, doctorName, cancellationToken);
            return Ok(plan);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
