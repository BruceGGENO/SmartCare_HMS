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

    public const string PatientsRead = "patients.read";
    public const string PatientsManage = "patients.manage";

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

    public const string StaffRead = "staff.read";

    public const string BillingRead = "billing.read";
    public const string BillingPay = "billing.pay";
    public const string RevenueRead = "revenue.read";
    public const string RevenueManage = "revenue.manage";

    public const string PharmacyRead = "pharmacy.read";
    public const string PharmacyDispense = "pharmacy.dispense";

    public const string LabRead = "lab.read";
    public const string LabManage = "lab.manage";

    public const string BedsRead = "beds.read";
    public const string BedsManage = "beds.manage";

    public const string DischargeRead = "discharge.read";
    public const string DischargeManage = "discharge.manage";

    public const string WaitingRoomRead = "waitingroom.read";
    public const string WaitingRoomManage = "waitingroom.manage";

    public const string OccupancyRead = "occupancy.read";

    public static readonly IReadOnlyList<string> All =
    [
        AuditRead,
        OutboxRead,
        DocumentsUpload,
        DocumentsRead,
        PatientsRead,
        PatientsManage,
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
        StaffRead,
        BillingRead,
        BillingPay,
        RevenueRead,
        RevenueManage,
        PharmacyRead,
        PharmacyDispense,
        LabRead,
        LabManage,
        BedsRead,
        BedsManage,
        DischargeRead,
        DischargeManage,
        WaitingRoomRead,
        WaitingRoomManage,
        OccupancyRead
    ];
}
