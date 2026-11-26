using System.Data.Common;
using Microsoft.Data.SqlClient;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Services;

public sealed class LabService : ILabService
{
    private readonly ISqlExecutor _sql;
    public LabService(ISqlExecutor sql) => _sql = sql;

    public Task<IReadOnlyList<LabTestResponse>> ListTestsAsync(CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_ListLabTests", r => new LabTestResponse
        {
            LabTestId = r.Get<int>("LabTestId"),
            Code = r.Get<string>("Code"),
            Name = r.Get<string>("Name"),
            Specimen = r.GetStringOrNull("Specimen")
        }, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<LabOrderResponse>> ListOrdersAsync(int? patientId, string? status, CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_ListLabOrders", MapOrder,
            new Dictionary<string, object?> { ["PatientId"] = patientId, ["Status"] = status }, cancellationToken);

    public async Task<LabOrderResponse?> GetOrderAsync(int labOrderId, CancellationToken cancellationToken = default)
    {
        var rows = await _sql.QueryAsync("dbo.sp_GetLabOrderById", MapOrder,
            new Dictionary<string, object?> { ["LabOrderId"] = labOrderId }, cancellationToken);
        return rows.Count == 0 ? null : rows[0];
    }

    public async Task<LabOrderResponse> CreateOrderAsync(int patientId, int labTestId, Guid? orderedByUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync("dbo.sp_CreateLabOrder", MapOrder,
                new Dictionary<string, object?>
                {
                    ["PatientId"] = patientId,
                    ["LabTestId"] = labTestId,
                    ["OrderedByUserId"] = orderedByUserId
                }, cancellationToken);
            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 50021 or 50022)
        {
            throw new ArgumentException(ex.Message.Trim());
        }
    }

    public async Task<LabOrderResponse> FileResultAsync(int labOrderId, string resultValue, string? notes, Guid? resultedByUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync("dbo.sp_FileLabResult", MapOrder,
                new Dictionary<string, object?>
                {
                    ["LabOrderId"] = labOrderId,
                    ["ResultValue"] = resultValue.Trim(),
                    ["ResultNotes"] = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                    ["ResultedByUserId"] = resultedByUserId
                }, cancellationToken);
            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 50023 or 50024)
        {
            throw new InvalidOperationException(ex.Message.Trim());
        }
    }

    private static LabOrderResponse MapOrder(DbDataReader r) => new()
    {
        LabOrderId = r.Get<int>("LabOrderId"),
        PatientId = r.Get<int>("PatientId"),
        PatientName = r.Get<string>("PatientName"),
        PatientMrn = r.Get<string>("PatientMrn"),
        LabTestId = r.Get<int>("LabTestId"),
        TestCode = r.Get<string>("TestCode"),
        TestName = r.Get<string>("TestName"),
        OrderedByUserId = r.GetOrNull<Guid>("OrderedByUserId"),
        Status = r.Get<string>("Status"),
        ResultValue = r.GetStringOrNull("ResultValue"),
        ResultNotes = r.GetStringOrNull("ResultNotes"),
        ResultedByUserId = r.GetOrNull<Guid>("ResultedByUserId"),
        OrderedAtUtc = SqlTime.Read(r, "OrderedAtUtc"),
        ResultedAtUtc = SqlTime.ReadOrNull(r, "ResultedAtUtc")
    };
}
