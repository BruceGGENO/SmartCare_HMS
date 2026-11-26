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
public sealed class PatientsController : ControllerBase
{
    private readonly IPatientService _patients;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;
    private readonly IValidator<RegisterPatientRequest> _registerValidator;
    private readonly IValidator<AddPatientAllergyRequest> _allergyValidator;
    private readonly IValidator<AddPatientDiagnosisRequest> _diagnosisValidator;

    public PatientsController(
        IPatientService patients,
        ICurrentUser currentUser,
        IAuditLogger audit,
        IValidator<RegisterPatientRequest> registerValidator,
        IValidator<AddPatientAllergyRequest> allergyValidator,
        IValidator<AddPatientDiagnosisRequest> diagnosisValidator)
    {
        _patients = patients;
        _currentUser = currentUser;
        _audit = audit;
        _registerValidator = registerValidator;
        _allergyValidator = allergyValidator;
        _diagnosisValidator = diagnosisValidator;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.PatientsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PatientResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken cancellationToken) =>
        Ok(await _patients.SearchAsync(q, cancellationToken));

    [HttpGet("icd")]
    [Authorize(Policy = AppPermissions.PatientsIcdRead)]
    [ProducesResponseType(typeof(IReadOnlyList<IcdCodeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchIcd([FromQuery] string? q, CancellationToken cancellationToken) =>
        Ok(await _patients.SearchIcdAsync(q, cancellationToken));

    [HttpGet("{id:int}")]
    [Authorize(Policy = AppPermissions.PatientsRead)]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetByIdAsync(id, cancellationToken);
        return patient is null ? NotFound() : Ok(patient);
    }

    [HttpPost]
    [Authorize(Policy = AppPermissions.PatientsRegister)]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(DuplicateMrnResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterPatientRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        var existing = await _patients.GetByMrnAsync(request.Mrn, cancellationToken);
        if (existing is not null)
        {
            return Conflict(new DuplicateMrnResponse { ExistingPatient = existing });
        }

        try
        {
            var created = await _patients.RegisterAsync(
                request.Mrn,
                request.FullName,
                request.DateOfBirth,
                request.Phone,
                request.Sex,
                request.UserId,
                cancellationToken);

            await _audit.LogAsync(new AuditEvent
            {
                Action = "Patient.Register",
                EntityType = "Patient",
                EntityId = created.PatientId.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.PatientId }, created);
        }
        catch (InvalidOperationException)
        {
            var raced = await _patients.GetByMrnAsync(request.Mrn, cancellationToken);
            if (raced is not null)
                return Conflict(new DuplicateMrnResponse { ExistingPatient = raced });

            return Conflict(new ApiMessageResponse { Message = "A patient with this MRN already exists." });
        }
    }

    [HttpGet("{id:int}/allergies")]
    [Authorize(Policy = AppPermissions.PatientsClinicalRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PatientAllergyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListAllergies(int id, CancellationToken cancellationToken)
    {
        if (await _patients.GetByIdAsync(id, cancellationToken) is null)
            return NotFound();

        return Ok(await _patients.ListAllergiesAsync(id, cancellationToken));
    }

    [HttpPost("{id:int}/allergies")]
    [Authorize(Policy = AppPermissions.PatientsClinicalWrite)]
    [ProducesResponseType(typeof(PatientAllergyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAllergy(
        int id,
        [FromBody] AddPatientAllergyRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _allergyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        if (await _patients.GetByIdAsync(id, cancellationToken) is null)
            return NotFound();

        var allergy = await _patients.AddAllergyAsync(id, request.Substance, request.Reaction, request.Severity, cancellationToken);

        await _audit.LogAsync(new AuditEvent
        {
            Action = "Patient.Allergy.Add",
            EntityType = "Patient",
            EntityId = id.ToString(),
            ActorUserId = _currentUser.UserId,
            Outcome = "Success"
        }, cancellationToken);

        return CreatedAtAction(nameof(ListAllergies), new { id }, allergy);
    }

    [HttpGet("{id:int}/diagnoses")]
    [Authorize(Policy = AppPermissions.PatientsClinicalRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PatientDiagnosisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListDiagnoses(int id, CancellationToken cancellationToken)
    {
        if (await _patients.GetByIdAsync(id, cancellationToken) is null)
            return NotFound();

        return Ok(await _patients.ListDiagnosesAsync(id, cancellationToken));
    }

    [HttpPost("{id:int}/diagnoses")]
    [Authorize(Policy = AppPermissions.PatientsClinicalWrite)]
    [ProducesResponseType(typeof(PatientDiagnosisResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddDiagnosis(
        int id,
        [FromBody] AddPatientDiagnosisRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _diagnosisValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        if (await _patients.GetByIdAsync(id, cancellationToken) is null)
            return NotFound();

        Guid? actorId = Guid.TryParse(_currentUser.UserId, out var uid) ? uid : null;

        try
        {
            var diagnosis = await _patients.AddDiagnosisAsync(
                id,
                request.IcdCode,
                request.Notes,
                actorId,
                cancellationToken);

            await _audit.LogAsync(new AuditEvent
            {
                Action = "Patient.Diagnosis.Add",
                EntityType = "Patient",
                EntityId = id.ToString(),
                ActorUserId = _currentUser.UserId,
                Outcome = "Success"
            }, cancellationToken);

            return CreatedAtAction(nameof(ListDiagnoses), new { id }, diagnosis);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiMessageResponse { Message = ex.Message });
        }
    }
}
