using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Services;

public sealed class PatientEhrService : IPatientEhrService
{
    private readonly ISqlExecutor _sql;
    private static readonly ConcurrentDictionary<Guid, PatientProfileDto> _ehrProfiles = new();
    private static readonly object _lock = new();

    static PatientEhrService()
    {
        // Seed initial rich EHR patient profiles
        var p1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        _ehrProfiles[p1Id] = new PatientProfileDto
        {
            Id = p1Id,
            Mrn = "SC-0001",
            FullName = "Demo Patient",
            FirstName = "Demo",
            LastName = "Patient",
            DateOfBirth = new DateTime(1994, 3, 12),
            Gender = "Female",
            PhoneNumber = "0780000001",
            Email = "patient@smartcare.org",
            Address = "Kigali, Gasabo, KG 123 St",
            BloodGroup = "O+",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-6),
            Allergies =
            [
                new AllergyDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = p1Id,
                    Substance = "Penicillin",
                    Severity = "Severe",
                    Reaction = "Anaphylaxis / Hives",
                    RecordedAtUtc = DateTimeOffset.UtcNow.AddMonths(-3)
                },
                new AllergyDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = p1Id,
                    Substance = "Ibuprofen",
                    Severity = "Moderate",
                    Reaction = "Gastric irritation / Angioedema",
                    RecordedAtUtc = DateTimeOffset.UtcNow.AddMonths(-2)
                }
            ],
            Diagnoses =
            [
                new DiagnosisDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = p1Id,
                    IcdCode = "J06.9",
                    Description = "Acute upper respiratory infection, unspecified",
                    DiagnosedBy = "Dr. Robert Mugabe",
                    DiagnosedAtUtc = DateTimeOffset.UtcNow.AddDays(-14)
                },
                new DiagnosisDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = p1Id,
                    IcdCode = "E11.9",
                    Description = "Type 2 diabetes mellitus without complications",
                    DiagnosedBy = "Dr. Sarah Mukamana",
                    DiagnosedAtUtc = DateTimeOffset.UtcNow.AddMonths(-5)
                }
            ],
            TreatmentPlans =
            [
                new TreatmentPlanDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = p1Id,
                    DoctorName = "Dr. Robert Mugabe",
                    Goals = "Maintain HbA1c < 6.5%, blood pressure < 130/80 mmHg, complete respiratory recovery.",
                    Medications = "Metformin 500mg BID, Salbutamol inhaler PRN, Vitamin C 500mg daily",
                    FollowUpDate = DateTime.UtcNow.AddDays(14),
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-14)
                }
            ],
            MedicalHistory =
            [
                new MedicalHistoryDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = p1Id,
                    Condition = "Seasonal allergic rhinitis",
                    PastSurgeries = "Appendectomy (2018)",
                    FamilyHistory = "Maternal diabetes type 2, paternal hypertension",
                    Notes = "Patient exercises 3 times weekly; nonsmoker.",
                    RecordedAtUtc = DateTimeOffset.UtcNow.AddMonths(-6)
                }
            ]
        };

        var p2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        _ehrProfiles[p2Id] = new PatientProfileDto
        {
            Id = p2Id,
            Mrn = "SC-0002",
            FullName = "Jean Uwimana",
            FirstName = "Jean",
            LastName = "Uwimana",
            DateOfBirth = new DateTime(1988, 7, 4),
            Gender = "Male",
            PhoneNumber = "0780000002",
            Email = "jean.u@smartcare.org",
            Address = "Kigali, Nyarugenge, KN 45 St",
            BloodGroup = "A+",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-4),
            Allergies =
            [
                new AllergyDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = p2Id,
                    Substance = "Sulfa Drugs",
                    Severity = "Severe",
                    Reaction = "Severe rash and Stevens-Johnson syndrome risk",
                    RecordedAtUtc = DateTimeOffset.UtcNow.AddMonths(-1)
                }
            ],
            Diagnoses =
            [
                new DiagnosisDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = p2Id,
                    IcdCode = "I10",
                    Description = "Essential (primary) hypertension",
                    DiagnosedBy = "Dr. Robert Mugabe",
                    DiagnosedAtUtc = DateTimeOffset.UtcNow.AddMonths(-2)
                }
            ],
            TreatmentPlans =
            [
                new TreatmentPlanDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = p2Id,
                    DoctorName = "Dr. Robert Mugabe",
                    Goals = "Reduce systolic blood pressure to < 125 mmHg. Sodium restricted diet.",
                    Medications = "Amlodipine 5mg daily PO morning",
                    FollowUpDate = DateTime.UtcNow.AddDays(30),
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-2)
                }
            ],
            MedicalHistory =
            [
                new MedicalHistoryDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = p2Id,
                    Condition = "Hypertension diagnosed 2022",
                    PastSurgeries = "None",
                    FamilyHistory = "Paternal stroke at age 62",
                    Notes = "Low sodium diet advised.",
                    RecordedAtUtc = DateTimeOffset.UtcNow.AddMonths(-4)
                }
            ]
        };
    }

    public PatientEhrService(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public async Task<IReadOnlyList<PatientProfileDto>> SearchPatientsAsync(string? query, CancellationToken cancellationToken = default)
    {
        var list = _ehrProfiles.Values.ToList();

        // Also sync from SQL dbo.Patient if present
        try
        {
            var sqlPatients = await _sql.QueryAsync(
                "dbo.sp_ListPatients",
                r => (
                    PatientId: r.Get<int>("PatientId"),
                    Mrn: r.Get<string>("Mrn"),
                    FullName: r.Get<string>("FullName"),
                    Phone: r.Get<string>("Phone"),
                    DateOfBirth: r.Get<DateTime>("DateOfBirth")
                ),
                cancellationToken: cancellationToken);

            foreach (var sp in sqlPatients)
            {
                var deterministicGuid = Guid.Parse($"00000000-0000-0000-0000-{sp.PatientId:D12}");
                if (!_ehrProfiles.ContainsKey(deterministicGuid))
                {
                    var names = sp.FullName.Split(' ', 2);
                    var newProfile = new PatientProfileDto
                    {
                        Id = deterministicGuid,
                        Mrn = sp.Mrn,
                        FullName = sp.FullName,
                        FirstName = names.Length > 0 ? names[0] : sp.FullName,
                        LastName = names.Length > 1 ? names[1] : "",
                        DateOfBirth = sp.DateOfBirth,
                        PhoneNumber = sp.Phone,
                        BloodGroup = "O+",
                        Gender = "Unspecified",
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    };
                    _ehrProfiles[deterministicGuid] = newProfile;
                }
            }
        }
        catch
        {
            // Fall back to in-memory store
        }

        if (string.IsNullOrWhiteSpace(query))
            return _ehrProfiles.Values.OrderByDescending(p => p.CreatedAtUtc).ToList();

        var q = query.Trim().ToLowerInvariant();
        return _ehrProfiles.Values
            .Where(p => p.FullName.ToLowerInvariant().Contains(q) ||
                        p.Mrn.ToLowerInvariant().Contains(q) ||
                        p.PhoneNumber.Contains(q))
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToList();
    }

    public Task<PatientProfileDto?> GetPatientByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _ehrProfiles.TryGetValue(id, out var profile);
        return Task.FromResult(profile);
    }

    public Task<PatientDuplicateCheckResult> CheckDuplicateAsync(
        string firstName,
        string lastName,
        DateTime dob,
        string phone,
        CancellationToken cancellationToken = default)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        var normalizedPhone = phone.Trim();

        var duplicate = _ehrProfiles.Values.FirstOrDefault(p =>
            string.Equals(p.FullName, fullName, StringComparison.OrdinalIgnoreCase) &&
            p.DateOfBirth.Date == dob.Date &&
            p.PhoneNumber.Trim() == normalizedPhone);

        if (duplicate is not null)
        {
            return Task.FromResult(new PatientDuplicateCheckResult
            {
                IsDuplicate = true,
                ExistingPatientId = duplicate.Id,
                ExistingMrn = duplicate.Mrn,
                Message = $"Warning: Potential duplicate patient found! {duplicate.FullName} (DOB: {duplicate.DateOfBirth:yyyy-MM-dd}, Phone: {duplicate.PhoneNumber}) is already registered with MRN {duplicate.Mrn}."
            });
        }

        return Task.FromResult(new PatientDuplicateCheckResult
        {
            IsDuplicate = false,
            Message = "No matching patient record found. Proceeding with registration."
        });
    }

    public async Task<PatientProfileDto> RegisterPatientAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default)
    {
        var newId = Guid.NewGuid();
        var count = _ehrProfiles.Count + 10;
        var mrn = $"MRN-2026-{count:D4}";
        var fullName = $"{request.FirstName} {request.LastName}".Trim();

        var profile = new PatientProfileDto
        {
            Id = newId,
            Mrn = mrn,
            FullName = fullName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Address = request.Address,
            BloodGroup = request.BloodGroup,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _ehrProfiles[newId] = profile;

        // Also insert into SQL dbo.Patient if possible
        try
        {
            await _sql.ExecuteNonQueryAsync(
                "dbo.sp_InsertPatient",
                new Dictionary<string, object?>
                {
                    ["UserId"] = null,
                    ["Mrn"] = mrn,
                    ["FullName"] = fullName,
                    ["DateOfBirth"] = request.DateOfBirth,
                    ["Phone"] = request.PhoneNumber
                },
                cancellationToken);
        }
        catch
        {
            // Retained in _ehrProfiles
        }

        return profile;
    }

    public Task<AllergyDto> AddAllergyAsync(Guid patientId, AddAllergyRequest request, CancellationToken cancellationToken = default)
    {
        if (!_ehrProfiles.TryGetValue(patientId, out var profile))
            throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

        var allergy = new AllergyDto
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Substance = request.Substance,
            Severity = request.Severity,
            Reaction = request.Reaction,
            RecordedAtUtc = DateTimeOffset.UtcNow
        };

        lock (_lock)
        {
            profile.Allergies.Add(allergy);
        }

        return Task.FromResult(allergy);
    }

    public Task<DiagnosisDto> AddDiagnosisAsync(Guid patientId, AddDiagnosisRequest request, string doctorName, CancellationToken cancellationToken = default)
    {
        if (!_ehrProfiles.TryGetValue(patientId, out var profile))
            throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

        var diagnosis = new DiagnosisDto
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            EncounterId = request.EncounterId,
            IcdCode = request.IcdCode,
            Description = request.Description,
            DiagnosedBy = doctorName,
            DiagnosedAtUtc = DateTimeOffset.UtcNow
        };

        lock (_lock)
        {
            profile.Diagnoses.Add(diagnosis);
        }

        return Task.FromResult(diagnosis);
    }

    public Task<TreatmentPlanDto> SaveTreatmentPlanAsync(Guid patientId, SaveTreatmentPlanRequest request, string doctorName, CancellationToken cancellationToken = default)
    {
        if (!_ehrProfiles.TryGetValue(patientId, out var profile))
            throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

        var plan = new TreatmentPlanDto
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorName = doctorName,
            Goals = request.Goals,
            Medications = request.Medications,
            FollowUpDate = request.FollowUpDate,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        lock (_lock)
        {
            foreach (var p in profile.TreatmentPlans)
                p.IsActive = false;

            profile.TreatmentPlans.Insert(0, plan);
        }

        return Task.FromResult(plan);
    }
}
