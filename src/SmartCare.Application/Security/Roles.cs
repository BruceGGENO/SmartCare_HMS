namespace SmartCare.Application.Security;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string HospitalAdmin = "HospitalAdmin";
    public const string Doctor = "Doctor";
    public const string Nurse = "Nurse";
    public const string Receptionist = "Receptionist";
    public const string LabTechnician = "LabTechnician";
    public const string Pharmacist = "Pharmacist";
    public const string BillingStaff = "BillingStaff";
    public const string Patient = "Patient";

    // Comma lists for [Authorize(Roles = ...)]
    public const string PharmacyAccess = Pharmacist + "," + HospitalAdmin + "," + SuperAdmin;
    public const string LabStaff = LabTechnician + "," + Nurse;

    public static readonly string[] All =
    {
        SuperAdmin, HospitalAdmin, Doctor, Nurse, Receptionist,
        LabTechnician, Pharmacist, BillingStaff, Patient
    };
}

public static class Policies
{
    public const string AdminOnly = "AdminOnly";                 // Super Admin + Hospital Admin
    public const string ClinicalStaff = "ClinicalStaff";         // Doctor + Nurse
    public const string FrontDesk = "FrontDesk";                 // Receptionist + admins
    public const string BillingAccess = "BillingAccess";
    public const string PharmacyAccess = "PharmacyAccess";
    public const string LabAccess = "LabAccess";
    public const string PatientPortal = "PatientPortal";
    public const string AnyStaff = "AnyStaff";                   // every role except Patient
}
