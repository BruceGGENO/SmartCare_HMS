using SmartCare.Application.DTOs.Responses;

namespace SmartCare.Application.Interfaces.Services;

public interface IAppointmentService
{
    Task EnsureDemoDataAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientListItemResponse>> ListPatientsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoctorListItemResponse>> ListDoctorsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentResponse>> ListAsync(
        Guid? doctorUserId,
        int? patientId,
        CancellationToken cancellationToken = default);

    Task<AppointmentResponse?> GetByIdAsync(int appointmentId, CancellationToken cancellationToken = default);

    Task<AppointmentResponse> BookAsync(
        int patientId,
        Guid doctorUserId,
        string department,
        DateTime scheduledAtUtc,
        CancellationToken cancellationToken = default);

    Task RescheduleAsync(int appointmentId, DateTime scheduledAtUtc, CancellationToken cancellationToken = default);

    Task CancelAsync(int appointmentId, string reason, CancellationToken cancellationToken = default);

    Task<AppointmentResponse?> CheckInAsync(string code, CancellationToken cancellationToken = default);

    Task<int?> FindPatientIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentResponse>> ListHistoryAsync(int patientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageBalanceResponse>> ListPackagesAsync(int patientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReleasedNoteResponse>> ListReleasedNotesAsync(int patientId, CancellationToken cancellationToken = default);
}
