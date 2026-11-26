using System.Data.Common;
using Microsoft.Data.SqlClient;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Services;

public sealed class AppointmentService : IAppointmentService
{
    private readonly ISqlExecutor _sql;
    private readonly ICheckInCodeGenerator _checkIn;
    private readonly IAuthService _auth;

    public AppointmentService(
        ISqlExecutor sql,
        ICheckInCodeGenerator checkIn,
        IAuthService auth)
    {
        _sql = sql;
        _checkIn = checkIn;
        _auth = auth;
    }

    public async Task EnsureDemoDataAsync(CancellationToken cancellationToken = default)
    {
        var count = await _sql.ExecuteScalarAsync<int>("dbo.sp_CountPatients", cancellationToken: cancellationToken);
        if (count > 0)
            return;

        var patientUser = await _auth.FindByUserNameAsync("patient", cancellationToken);
        var portalPatientId = await InsertPatientAsync(
            patientUser?.UserId,
            "SC-0001",
            "Demo Patient",
            new DateTime(1994, 3, 12),
            "0780000001",
            cancellationToken);

        await InsertPatientAsync(null, "SC-0002", "Jean Uwimana", new DateTime(1988, 7, 4), "0780000002", cancellationToken);

        if (portalPatientId is > 0)
        {
            await _sql.ExecuteNonQueryAsync(
                "dbo.sp_InsertTreatmentPackage",
                new Dictionary<string, object?>
                {
                    ["PatientId"] = portalPatientId,
                    ["Name"] = "Physiotherapy pack",
                    ["SessionsPurchased"] = 8,
                    ["SessionsConsumed"] = 3
                },
                cancellationToken);

            await _sql.ExecuteNonQueryAsync(
                "dbo.sp_InsertReleasedNote",
                new Dictionary<string, object?>
                {
                    ["PatientId"] = portalPatientId,
                    ["AuthorName"] = "Dr. Demo",
                    ["Body"] = "Continue home exercises. Follow-up in two weeks. This note is marked shareable with the patient."
                },
                cancellationToken);
        }
    }

    public Task<IReadOnlyList<PatientListItemResponse>> ListPatientsAsync(CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_ListPatients", r => new PatientListItemResponse
        {
            PatientId = r.Get<int>("PatientId"),
            Mrn = r.Get<string>("Mrn"),
            FullName = r.Get<string>("FullName")
        }, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<DoctorListItemResponse>> ListDoctorsAsync(CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_ListDoctors", r => new DoctorListItemResponse
        {
            UserId = r.Get<Guid>("UserId"),
            DisplayName = r.Get<string>("DisplayName")
        }, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<AppointmentResponse>> ListAsync(
        Guid? doctorUserId,
        int? patientId,
        CancellationToken cancellationToken = default) =>
        _sql.QueryAsync(
            "dbo.sp_ListAppointments",
            MapAppointment,
            new Dictionary<string, object?>
            {
                ["DoctorUserId"] = doctorUserId,
                ["PatientId"] = patientId
            },
            cancellationToken);

    public async Task<AppointmentResponse?> GetByIdAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_GetAppointmentById",
            MapAppointment,
            new Dictionary<string, object?> { ["AppointmentId"] = appointmentId },
            cancellationToken);
        return rows.Count == 0 ? null : AttachQr(rows[0]);
    }

    public async Task<AppointmentResponse> BookAsync(
        int patientId,
        Guid doctorUserId,
        string department,
        DateTime scheduledAtUtc,
        CancellationToken cancellationToken = default)
    {
        var publicId = Guid.NewGuid();
        var checkIn = _checkIn.Create(publicId.ToString());

        int newId;
        try
        {
            var id = await _sql.ExecuteScalarAsync<int>(
                "dbo.sp_InsertAppointment",
                new Dictionary<string, object?>
                {
                    ["PublicId"] = publicId,
                    ["PatientId"] = patientId,
                    ["DoctorUserId"] = doctorUserId,
                    ["Department"] = department,
                    ["ScheduledAtUtc"] = DateTime.SpecifyKind(scheduledAtUtc, DateTimeKind.Utc),
                    ["CheckInCode"] = checkIn.Code
                },
                cancellationToken);

            newId = id;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("That doctor already has a booking in this slot.");
        }

        var patients = await ListPatientsAsync(cancellationToken);
        var patient = patients.FirstOrDefault(p => p.PatientId == patientId);

        await _sql.ExecuteNonQueryAsync(
            "dbo.sp_InsertOutbox",
            new Dictionary<string, object?>
            {
                ["Channel"] = "SMS",
                ["Recipient"] = patient?.Mrn ?? patientId.ToString(),
                ["Subject"] = "Appointment reminder",
                ["Body"] = $"Reminder: appointment {checkIn.Code} on {scheduledAtUtc:u}. Reply is not required. (stub)"
            },
            cancellationToken);

        return (await GetByIdAsync(newId, cancellationToken))!;
    }

    public async Task RescheduleAsync(int appointmentId, DateTime scheduledAtUtc, CancellationToken cancellationToken = default)
    {
        try
        {
            await _sql.ExecuteNonQueryAsync(
                "dbo.sp_RescheduleAppointment",
                new Dictionary<string, object?>
                {
                    ["AppointmentId"] = appointmentId,
                    ["ScheduledAtUtc"] = DateTime.SpecifyKind(scheduledAtUtc, DateTimeKind.Utc)
                },
                cancellationToken);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("That doctor already has a booking in this slot.");
        }
    }

    public Task CancelAsync(int appointmentId, string reason, CancellationToken cancellationToken = default) =>
        _sql.ExecuteNonQueryAsync(
            "dbo.sp_CancelAppointment",
            new Dictionary<string, object?>
            {
                ["AppointmentId"] = appointmentId,
                ["CancelReason"] = reason
            },
            cancellationToken);

    public async Task<AppointmentResponse?> CheckInAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalised = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalised.Length != 8)
            return null;

        var rows = await _sql.QueryAsync(
            "dbo.sp_CheckInAppointment",
            MapAppointment,
            new Dictionary<string, object?> { ["CheckInCode"] = normalised },
            cancellationToken);

        return rows.Count == 0 ? null : AttachQr(rows[0]);
    }

    public async Task<int?> FindPatientIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_FindPatientIdByUserId",
            r => r.Get<int>("PatientId"),
            new Dictionary<string, object?> { ["UserId"] = userId },
            cancellationToken);
        return rows.Count == 0 ? null : rows[0];
    }

    public Task<IReadOnlyList<AppointmentResponse>> ListHistoryAsync(int patientId, CancellationToken cancellationToken = default) =>
        _sql.QueryAsync(
            "dbo.sp_ListAppointmentHistory",
            MapAppointment,
            new Dictionary<string, object?> { ["PatientId"] = patientId },
            cancellationToken);

    public Task<IReadOnlyList<PackageBalanceResponse>> ListPackagesAsync(int patientId, CancellationToken cancellationToken = default) =>
        _sql.QueryAsync(
            "dbo.sp_ListPatientPackages",
            r => new PackageBalanceResponse
            {
                PackagePurchaseId = r.Get<int>("PackagePurchaseId"),
                Name = r.Get<string>("Name"),
                SessionsPurchased = r.Get<int>("SessionsPurchased"),
                SessionsConsumed = r.Get<int>("SessionsConsumed")
            },
            new Dictionary<string, object?> { ["PatientId"] = patientId },
            cancellationToken);

    public Task<IReadOnlyList<ReleasedNoteResponse>> ListReleasedNotesAsync(int patientId, CancellationToken cancellationToken = default) =>
        _sql.QueryAsync(
            "dbo.sp_ListReleasedNotes",
            r => new ReleasedNoteResponse
            {
                NoteId = r.Get<int>("NoteId"),
                CreatedAtUtc = ReadTime(r, "CreatedAtUtc"),
                AuthorName = r.Get<string>("AuthorName"),
                Body = r.Get<string>("Body")
            },
            new Dictionary<string, object?> { ["PatientId"] = patientId },
            cancellationToken);

    private AppointmentResponse AttachQr(AppointmentResponse appointment)
    {
        appointment.CheckInQrSvg = _checkIn.Create(appointment.PublicId.ToString()).SvgMarkup;
        return appointment;
    }

    private async Task<int> InsertPatientAsync(
        Guid? userId,
        string mrn,
        string fullName,
        DateTime? dob,
        string? phone,
        CancellationToken cancellationToken)
    {
        var id = await _sql.ExecuteScalarAsync<int>(
            "dbo.sp_InsertPatient",
            new Dictionary<string, object?>
            {
                ["UserId"] = userId,
                ["Mrn"] = mrn,
                ["FullName"] = fullName,
                ["DateOfBirth"] = dob,
                ["Phone"] = phone
            },
            cancellationToken);
        return id;
    }

    private static AppointmentResponse MapAppointment(DbDataReader r) => new()
    {
        Id = r.Get<int>("AppointmentId"),
        PublicId = r.Get<Guid>("PublicId"),
        PatientId = r.Get<int>("PatientId"),
        PatientName = r.Get<string>("PatientName"),
        PatientMrn = r.Get<string>("PatientMrn"),
        DoctorUserId = r.Get<Guid>("DoctorUserId"),
        DoctorName = r.Get<string>("DoctorName"),
        Department = r.Get<string>("Department"),
        ScheduledAtUtc = DateTime.SpecifyKind(r.Get<DateTime>("ScheduledAtUtc"), DateTimeKind.Utc),
        Status = r.Get<string>("Status"),
        CancelReason = r.GetStringOrNull("CancelReason"),
        CheckInCode = r.Get<string>("CheckInCode").Trim()
    };

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
