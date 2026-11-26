namespace SmartCare.Domain.Constants;

/// <summary>
/// Permission codes checked by API policies. Roles grant these via RolePermission.
/// </summary>
public static class AppPermissions
{
    public const string ClaimType = "permission";

    public const string AuditRead = "audit.read";
    public const string OutboxRead = "outbox.read";
    public const string DocumentsUpload = "documents.upload";
    public const string DocumentsRead = "documents.read";

    public const string AppointmentsRead = "appointments.read";
    public const string AppointmentsBook = "appointments.book";
    public const string AppointmentsReschedule = "appointments.reschedule";
    public const string AppointmentsCancel = "appointments.cancel";
    public const string AppointmentsCheckIn = "appointments.checkin";
    public const string AppointmentsPatientsRead = "appointments.patients.read";
    public const string AppointmentsDoctorsRead = "appointments.doctors.read";

    public const string PortalAppointmentsRead = "portal.appointments.read";
    public const string PortalHistoryRead = "portal.history.read";
    public const string PortalPackagesRead = "portal.packages.read";
    public const string PortalOtpLogin = "portal.otp.login";

    public const string BillingRead = "billing.read";
    public const string BillingCreate = "billing.create";
    public const string BillingPay = "billing.pay";

    public static readonly IReadOnlyList<string> All =
    [
        AuditRead,
        OutboxRead,
        DocumentsUpload,
        DocumentsRead,
        AppointmentsRead,
        AppointmentsBook,
        AppointmentsReschedule,
        AppointmentsCancel,
        AppointmentsCheckIn,
        AppointmentsPatientsRead,
        AppointmentsDoctorsRead,
        PortalAppointmentsRead,
        PortalHistoryRead,
        PortalPackagesRead,
        PortalOtpLogin,
        BillingRead,
        BillingCreate,
        BillingPay
    ];
}
