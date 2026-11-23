namespace SmartCare.Application.Common
{
    public static class AppRoles
    {
        public const string SuperAdmin = "Super Admin";
        public const string HospitalAdmin = "Hospital Admin";
        public const string Doctor = "Doctor";
        public const string Nurse = "Nurse";
        public const string Receptionist = "Receptionist";
        public const string LabTechnician = "Lab Technician";
        public const string Pharmacist = "Pharmacist";
        public const string BillingStaff = "Billing Staff";
        public const string Patient = "Patient";

        public static readonly IReadOnlyList<string> All =
        [
            SuperAdmin,
            HospitalAdmin,
            Doctor,
            Nurse,
            Receptionist,
            LabTechnician,
            Pharmacist,
            BillingStaff,
            Patient
        ];
    }
}
