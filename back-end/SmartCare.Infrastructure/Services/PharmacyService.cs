using System.Data.Common;
using Microsoft.Data.SqlClient;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Services;

public sealed class PharmacyService : IPharmacyService
{
    private readonly ISqlExecutor _sql;
    public PharmacyService(ISqlExecutor sql) => _sql = sql;

    public Task<IReadOnlyList<DrugResponse>> ListDrugsAsync(CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_ListDrugs", r => new DrugResponse
        {
            DrugId = r.Get<int>("DrugId"),
            Code = r.Get<string>("Code"),
            Name = r.Get<string>("Name"),
            Strength = r.GetStringOrNull("Strength"),
            StockQty = r.Get<int>("StockQty")
        }, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<PrescriptionResponse>> ListPrescriptionsAsync(int? patientId, string? status, CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_ListPrescriptions", MapRx,
            new Dictionary<string, object?> { ["PatientId"] = patientId, ["Status"] = status }, cancellationToken);

    public async Task<PrescriptionResponse?> GetPrescriptionAsync(int prescriptionId, CancellationToken cancellationToken = default)
    {
        var rows = await _sql.QueryAsync("dbo.sp_GetPrescriptionById", MapRx,
            new Dictionary<string, object?> { ["PrescriptionId"] = prescriptionId }, cancellationToken);
        return rows.Count == 0 ? null : rows[0];
    }

    public async Task<PrescriptionResponse> PrescribeAsync(int patientId, int drugId, string dose, int quantity, Guid? prescribedByUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync("dbo.sp_CreatePrescription", MapRx,
                new Dictionary<string, object?>
                {
                    ["PatientId"] = patientId,
                    ["DrugId"] = drugId,
                    ["Dose"] = dose.Trim(),
                    ["Quantity"] = quantity,
                    ["PrescribedByUserId"] = prescribedByUserId
                }, cancellationToken);
            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 50031 or 50032 or 50033)
        {
            throw new ArgumentException(ex.Message.Trim());
        }
    }

    public async Task<PrescriptionResponse> DispenseAsync(int prescriptionId, Guid? dispensedByUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync("dbo.sp_DispensePrescription", MapRx,
                new Dictionary<string, object?>
                {
                    ["PrescriptionId"] = prescriptionId,
                    ["DispensedByUserId"] = dispensedByUserId
                }, cancellationToken);
            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 50034 or 50035 or 50036)
        {
            throw new InvalidOperationException(ex.Message.Trim());
        }
    }

    private static PrescriptionResponse MapRx(DbDataReader r) => new()
    {
        PrescriptionId = r.Get<int>("PrescriptionId"),
        PatientId = r.Get<int>("PatientId"),
        PatientName = r.Get<string>("PatientName"),
        PatientMrn = r.Get<string>("PatientMrn"),
        DrugId = r.Get<int>("DrugId"),
        DrugCode = r.Get<string>("DrugCode"),
        DrugName = r.Get<string>("DrugName"),
        Strength = r.GetStringOrNull("Strength"),
        Dose = r.Get<string>("Dose"),
        Quantity = r.Get<int>("Quantity"),
        PrescribedByUserId = r.GetOrNull<Guid>("PrescribedByUserId"),
        Status = r.Get<string>("Status"),
        PrescribedAtUtc = SqlTime.Read(r, "PrescribedAtUtc"),
        DispensedAtUtc = SqlTime.ReadOrNull(r, "DispensedAtUtc"),
        DispensedByUserId = r.GetOrNull<Guid>("DispensedByUserId")
    };
}
