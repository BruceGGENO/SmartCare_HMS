using System.Data.Common;
using Microsoft.Data.SqlClient;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Services;

public sealed class BedService : IBedService
{
    private readonly ISqlExecutor _sql;
    public BedService(ISqlExecutor sql) => _sql = sql;

    public Task<IReadOnlyList<WardResponse>> ListWardsAsync(CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_ListWards", r => new WardResponse
        {
            WardId = r.Get<int>("WardId"),
            Name = r.Get<string>("Name"),
            Code = r.Get<string>("Code")
        }, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<BedResponse>> ListBedsAsync(int? wardId, CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_ListBeds", r => new BedResponse
        {
            BedId = r.Get<int>("BedId"),
            WardId = r.Get<int>("WardId"),
            WardName = r.Get<string>("WardName"),
            Code = r.Get<string>("Code"),
            Status = r.Get<string>("Status")
        }, new Dictionary<string, object?> { ["WardId"] = wardId }, cancellationToken);

    public Task<IReadOnlyList<AdmissionResponse>> ListAdmissionsAsync(string? status, CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_ListAdmissions", MapAdmission,
            new Dictionary<string, object?> { ["Status"] = status }, cancellationToken);

    public async Task<AdmissionResponse?> GetAdmissionAsync(int admissionId, CancellationToken cancellationToken = default)
    {
        var rows = await _sql.QueryAsync("dbo.sp_GetAdmissionById", MapAdmission,
            new Dictionary<string, object?> { ["AdmissionId"] = admissionId }, cancellationToken);
        return rows.Count == 0 ? null : rows[0];
    }

    public async Task<AdmissionResponse> AdmitAsync(int patientId, int bedId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync("dbo.sp_AdmitPatient", MapAdmission,
                new Dictionary<string, object?> { ["PatientId"] = patientId, ["BedId"] = bedId }, cancellationToken);
            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 50041 or 50042 or 50043)
        {
            throw new InvalidOperationException(ex.Message.Trim());
        }
    }

    public async Task<AdmissionResponse> TransferAsync(int admissionId, int newBedId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync("dbo.sp_TransferAdmission", MapAdmission,
                new Dictionary<string, object?> { ["AdmissionId"] = admissionId, ["NewBedId"] = newBedId }, cancellationToken);
            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 50043 or 50044 or 50045)
        {
            throw new InvalidOperationException(ex.Message.Trim());
        }
    }

    public async Task<AdmissionResponse> FreeBedAsync(int admissionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync("dbo.sp_FreeAdmissionBed", MapAdmission,
                new Dictionary<string, object?> { ["AdmissionId"] = admissionId }, cancellationToken);
            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 50044 or 50045)
        {
            throw new InvalidOperationException(ex.Message.Trim());
        }
    }

    private static AdmissionResponse MapAdmission(DbDataReader r) => new()
    {
        AdmissionId = r.Get<int>("AdmissionId"),
        PatientId = r.Get<int>("PatientId"),
        PatientName = r.Get<string>("PatientName"),
        PatientMrn = r.Get<string>("PatientMrn"),
        BedId = r.Get<int>("BedId"),
        BedCode = r.Get<string>("BedCode"),
        WardName = r.Get<string>("WardName"),
        Status = r.Get<string>("Status"),
        AdmittedAtUtc = SqlTime.Read(r, "AdmittedAtUtc"),
        DischargedAtUtc = SqlTime.ReadOrNull(r, "DischargedAtUtc")
    };
}
