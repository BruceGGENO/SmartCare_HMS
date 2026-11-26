using System.Data.Common;
using Microsoft.Data.SqlClient;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Services;

public sealed class PatientService : IPatientService
{
    private readonly ISqlExecutor _sql;

    public PatientService(ISqlExecutor sql) => _sql = sql;

    public Task<IReadOnlyList<PatientResponse>> SearchAsync(string? query, CancellationToken cancellationToken = default) =>
        _sql.QueryAsync(
            "dbo.sp_SearchPatients",
            MapPatient,
            new Dictionary<string, object?> { ["Query"] = string.IsNullOrWhiteSpace(query) ? null : query.Trim() },
            cancellationToken);

    public async Task<PatientResponse?> GetByIdAsync(int patientId, CancellationToken cancellationToken = default)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_GetPatientById",
            MapPatient,
            new Dictionary<string, object?> { ["PatientId"] = patientId },
            cancellationToken);
        return rows.Count == 0 ? null : rows[0];
    }

    public async Task<PatientResponse?> GetByMrnAsync(string mrn, CancellationToken cancellationToken = default)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_GetPatientByMrn",
            MapPatient,
            new Dictionary<string, object?> { ["Mrn"] = mrn.Trim() },
            cancellationToken);
        return rows.Count == 0 ? null : rows[0];
    }

    public async Task<PatientResponse> RegisterAsync(
        string mrn,
        string fullName,
        DateOnly? dateOfBirth,
        string? phone,
        string? sex,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync(
                "dbo.sp_RegisterPatient",
                MapPatient,
                new Dictionary<string, object?>
                {
                    ["UserId"] = userId,
                    ["Mrn"] = mrn.Trim(),
                    ["FullName"] = fullName.Trim(),
                    ["DateOfBirth"] = dateOfBirth?.ToDateTime(TimeOnly.MinValue),
                    ["Phone"] = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                    ["Sex"] = string.IsNullOrWhiteSpace(sex) ? null : sex.Trim()
                },
                cancellationToken);

            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("A patient with this MRN already exists.");
        }
    }

    public Task<IReadOnlyList<PatientAllergyResponse>> ListAllergiesAsync(
        int patientId,
        CancellationToken cancellationToken = default) =>
        _sql.QueryAsync(
            "dbo.sp_ListPatientAllergies",
            MapAllergy,
            new Dictionary<string, object?> { ["PatientId"] = patientId },
            cancellationToken);

    public async Task<PatientAllergyResponse> AddAllergyAsync(
        int patientId,
        string substance,
        string? reaction,
        string? severity,
        CancellationToken cancellationToken = default)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_InsertPatientAllergy",
            MapAllergy,
            new Dictionary<string, object?>
            {
                ["PatientId"] = patientId,
                ["Substance"] = substance.Trim(),
                ["Reaction"] = string.IsNullOrWhiteSpace(reaction) ? null : reaction.Trim(),
                ["Severity"] = string.IsNullOrWhiteSpace(severity) ? null : severity.Trim()
            },
            cancellationToken);

        return rows[0];
    }

    public Task<IReadOnlyList<IcdCodeResponse>> SearchIcdAsync(string? query, CancellationToken cancellationToken = default) =>
        _sql.QueryAsync(
            "dbo.sp_SearchIcdCodes",
            r => new IcdCodeResponse
            {
                Code = r.Get<string>("Code"),
                Description = r.Get<string>("Description")
            },
            new Dictionary<string, object?> { ["Query"] = string.IsNullOrWhiteSpace(query) ? null : query.Trim() },
            cancellationToken);

    public Task<IReadOnlyList<PatientDiagnosisResponse>> ListDiagnosesAsync(
        int patientId,
        CancellationToken cancellationToken = default) =>
        _sql.QueryAsync(
            "dbo.sp_ListPatientDiagnoses",
            MapDiagnosis,
            new Dictionary<string, object?> { ["PatientId"] = patientId },
            cancellationToken);

    public async Task<PatientDiagnosisResponse> AddDiagnosisAsync(
        int patientId,
        string icdCode,
        string? notes,
        Guid? diagnosedByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync(
                "dbo.sp_InsertPatientDiagnosis",
                MapDiagnosis,
                new Dictionary<string, object?>
                {
                    ["PatientId"] = patientId,
                    ["IcdCode"] = icdCode.Trim().ToUpperInvariant(),
                    ["Notes"] = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                    ["DiagnosedByUserId"] = diagnosedByUserId
                },
                cancellationToken);

            return rows[0];
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            throw new ArgumentException("Unknown ICD code. Search /api/patients/icd and pick a valid code.");
        }
    }

    private static PatientResponse MapPatient(DbDataReader r) => new()
    {
        PatientId = r.Get<int>("PatientId"),
        UserId = r.GetOrNull<Guid>("UserId"),
        Mrn = r.Get<string>("Mrn"),
        FullName = r.Get<string>("FullName"),
        DateOfBirth = ReadDateOnly(r, "DateOfBirth"),
        Phone = r.GetStringOrNull("Phone"),
        Sex = r.GetStringOrNull("Sex"),
        CreatedAtUtc = ReadTime(r, "CreatedAtUtc")
    };

    private static PatientAllergyResponse MapAllergy(DbDataReader r) => new()
    {
        AllergyId = r.Get<int>("AllergyId"),
        PatientId = r.Get<int>("PatientId"),
        Substance = r.Get<string>("Substance"),
        Reaction = r.GetStringOrNull("Reaction"),
        Severity = r.GetStringOrNull("Severity"),
        NotedAtUtc = ReadTime(r, "NotedAtUtc")
    };

    private static PatientDiagnosisResponse MapDiagnosis(DbDataReader r) => new()
    {
        DiagnosisId = r.Get<int>("DiagnosisId"),
        PatientId = r.Get<int>("PatientId"),
        IcdCode = r.Get<string>("IcdCode"),
        IcdDescription = r.Get<string>("IcdDescription"),
        Notes = r.GetStringOrNull("Notes"),
        DiagnosedByUserId = r.GetOrNull<Guid>("DiagnosedByUserId"),
        DiagnosedAtUtc = ReadTime(r, "DiagnosedAtUtc")
    };

    private static DateOnly? ReadDateOnly(DbDataReader record, string column)
    {
        var ordinal = record.GetOrdinal(column);
        if (record.IsDBNull(ordinal))
            return null;

        var value = record.GetValue(ordinal);
        return value switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            _ => DateOnly.FromDateTime(Convert.ToDateTime(value))
        };
    }

    private static DateTimeOffset ReadTime(DbDataReader record, string column)
    {
        var value = record.GetValue(record.GetOrdinal(column));
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => DateTimeOffset.UtcNow
        };
    }
}
