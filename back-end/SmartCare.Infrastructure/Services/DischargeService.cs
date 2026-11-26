using System.Data.Common;
using Microsoft.Data.SqlClient;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Services;

public sealed class DischargeService : IDischargeService
{
    private readonly ISqlExecutor _sql;
    public DischargeService(ISqlExecutor sql) => _sql = sql;

    public async Task<IReadOnlyList<DischargeCaseResponse>> ListAsync(string? status, CancellationToken cancellationToken = default)
    {
        var cases = await _sql.QueryAsync("dbo.sp_ListDischargeCases", MapCase,
            new Dictionary<string, object?> { ["Status"] = status }, cancellationToken);
        var result = new List<DischargeCaseResponse>();
        foreach (var c in cases)
        {
            c.Steps = await ListStepsAsync(c.DischargeId, cancellationToken);
            result.Add(c);
        }
        return result;
    }

    public async Task<DischargeCaseResponse?> GetByIdAsync(int dischargeId, CancellationToken cancellationToken = default)
    {
        var rows = await _sql.QueryAsync("dbo.sp_GetDischargeById", MapCase,
            new Dictionary<string, object?> { ["DischargeId"] = dischargeId }, cancellationToken);
        if (rows.Count == 0) return null;
        var item = rows[0];
        item.Steps = await ListStepsAsync(dischargeId, cancellationToken);
        return item;
    }

    public async Task<DischargeCaseResponse> InitiateAsync(int admissionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync("dbo.sp_InitiateDischarge", MapCase,
                new Dictionary<string, object?> { ["AdmissionId"] = admissionId }, cancellationToken);
            var created = rows[0];
            created.Steps = await ListStepsAsync(created.DischargeId, cancellationToken);
            return created;
        }
        catch (SqlException ex) when (ex.Number is 50051 or 50052 or 50053)
        {
            throw new InvalidOperationException(ex.Message.Trim());
        }
    }

    public async Task<DischargeStepResponse> CompleteStepAsync(int dischargeStepId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync("dbo.sp_CompleteDischargeStep", MapStep,
                new Dictionary<string, object?> { ["DischargeStepId"] = dischargeStepId }, cancellationToken);
            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 50054 or 50055 or 50056)
        {
            throw new InvalidOperationException(ex.Message.Trim());
        }
    }

    public async Task<DischargeCaseResponse> CompleteAsync(int dischargeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync("dbo.sp_CompleteDischarge", MapCase,
                new Dictionary<string, object?> { ["DischargeId"] = dischargeId }, cancellationToken);
            var item = rows[0];
            item.Steps = await ListStepsAsync(dischargeId, cancellationToken);
            return item;
        }
        catch (SqlException ex) when (ex.Number is 50056 or 50057 or 50058)
        {
            throw new InvalidOperationException(ex.Message.Trim());
        }
    }

    private Task<IReadOnlyList<DischargeStepResponse>> ListStepsAsync(int dischargeId, CancellationToken cancellationToken) =>
        _sql.QueryAsync("dbo.sp_ListDischargeSteps", MapStep,
            new Dictionary<string, object?> { ["DischargeId"] = dischargeId }, cancellationToken);

    private static DischargeCaseResponse MapCase(DbDataReader r) => new()
    {
        DischargeId = r.Get<int>("DischargeId"),
        AdmissionId = r.Get<int>("AdmissionId"),
        PatientId = r.Get<int>("PatientId"),
        PatientName = r.Get<string>("PatientName"),
        PatientMrn = r.Get<string>("PatientMrn"),
        Status = r.Get<string>("Status"),
        CreatedAtUtc = SqlTime.Read(r, "CreatedAtUtc"),
        CompletedAtUtc = SqlTime.ReadOrNull(r, "CompletedAtUtc")
    };

    private static DischargeStepResponse MapStep(DbDataReader r) => new()
    {
        DischargeStepId = r.Get<int>("DischargeStepId"),
        DischargeId = r.Get<int>("DischargeId"),
        StepName = r.Get<string>("StepName"),
        SortOrder = r.Get<int>("SortOrder"),
        IsComplete = Convert.ToBoolean(r.GetValue(r.GetOrdinal("IsComplete"))),
        CompletedAtUtc = SqlTime.ReadOrNull(r, "CompletedAtUtc")
    };
}
