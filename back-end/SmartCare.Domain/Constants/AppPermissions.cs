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

    public const string PatientsRead = "patients.read";
    public const string PatientsRegister = "patients.register";
    public const string PatientsClinicalRead = "patients.clinical.read";
    public const string PatientsClinicalWrite = "patients.clinical.write";
    public const string PatientsIcdRead = "patients.icd.read";

    public const string BillingRead = "billing.read";
    public const string BillingCreate = "billing.create";
    public const string BillingPay = "billing.pay";

    public const string RevenueRead = "revenue.read";
    public const string RevenuePurchase = "revenue.purchase";
    public const string RevenueConsume = "revenue.consume";

    public const string LabRead = "lab.read";
    public const string LabOrder = "lab.order";
    public const string LabResult = "lab.result";

    public const string PharmacyRead = "pharmacy.read";
    public const string PharmacyPrescribe = "pharmacy.prescribe";
    public const string PharmacyDispense = "pharmacy.dispense";

    public const string BedsRead = "beds.read";
    public const string BedsAssign = "beds.assign";

    public const string DischargeRead = "discharge.read";
    public const string DischargeWrite = "discharge.write";

    public const string WaitingRead = "waiting.read";
    public const string WaitingManage = "waiting.manage";

    public const string ForecastRead = "forecast.read";

    public static readonly IReadOnlyList<string> All =
    [
        AuditRead, OutboxRead, DocumentsUpload, DocumentsRead,
        AppointmentsRead, AppointmentsBook, AppointmentsReschedule, AppointmentsCancel,
        AppointmentsCheckIn, AppointmentsPatientsRead, AppointmentsDoctorsRead,
        PortalAppointmentsRead, PortalHistoryRead, PortalPackagesRead, PortalOtpLogin,
        PatientsRead, PatientsRegister, PatientsClinicalRead, PatientsClinicalWrite, PatientsIcdRead,
        BillingRead, BillingCreate, BillingPay,
        RevenueRead, RevenuePurchase, RevenueConsume,
        LabRead, LabOrder, LabResult,
        PharmacyRead, PharmacyPrescribe, PharmacyDispense,
        BedsRead, BedsAssign,
        DischargeRead, DischargeWrite,
        WaitingRead, WaitingManage,
        ForecastRead
    ];
}
