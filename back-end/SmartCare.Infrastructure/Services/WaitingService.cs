using System.Data.Common;
using Microsoft.Data.SqlClient;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Services;

public sealed class WaitingService : IWaitingService
{
    private readonly ISqlExecutor _sql;
    public WaitingService(ISqlExecutor sql) => _sql = sql;

    public Task<IReadOnlyList<WaitingQueueItemResponse>> ListQueueAsync(string? department, string? station, CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_ListWaitingQueue", Map,
            new Dictionary<string, object?> { ["Department"] = department, ["Station"] = station }, cancellationToken);

    public async Task<WaitingQueueItemResponse> EnterQueueAsync(int appointmentId, string station, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync("dbo.sp_EnterWaitingQueue", Map,
                new Dictionary<string, object?> { ["AppointmentId"] = appointmentId, ["Station"] = station.Trim() }, cancellationToken);
            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 50061 or 50062)
        {
            throw new InvalidOperationException(ex.Message.Trim());
        }
    }

    public async Task<WaitingQueueItemResponse> CallAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _sql.QueryAsync("dbo.sp_CallWaitingPatient", Map,
                new Dictionary<string, object?> { ["AppointmentId"] = appointmentId }, cancellationToken);
            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 50061 or 50063)
        {
            throw new InvalidOperationException(ex.Message.Trim());
        }
    }

    private static WaitingQueueItemResponse Map(DbDataReader r) => new()
    {
        AppointmentId = r.Get<int>("AppointmentId"),
        PublicId = r.Get<Guid>("PublicId"),
        PatientId = r.Get<int>("PatientId"),
        PatientName = r.Get<string>("PatientName"),
        PatientMrn = r.Get<string>("PatientMrn"),
        DoctorUserId = r.Get<Guid>("DoctorUserId"),
        DoctorName = r.Get<string>("DoctorName"),
        Department = r.Get<string>("Department"),
        ScheduledAtUtc = DateTime.SpecifyKind(r.Get<DateTime>("ScheduledAtUtc"), DateTimeKind.Utc),
        Status = r.Get<string>("Status"),
        CheckInCode = r.Get<string>("CheckInCode").Trim(),
        QueueStation = r.GetStringOrNull("QueueStation"),
        QueueEnteredAtUtc = SqlTime.ReadDateTimeOrNull(r, "QueueEnteredAtUtc"),
        CalledAtUtc = SqlTime.ReadDateTimeOrNull(r, "CalledAtUtc")
    };
}
