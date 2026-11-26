using System.Text;
using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.Infrastructure.Services;

public sealed class StaffService : IStaffService
{
    private static readonly List<StaffProfileDto> _staff =
    [
        new StaffProfileDto { Id = Guid.NewGuid(), UserName = "doctor", FullName = "Dr. Robert Mugabe", Role = AppRoles.Doctor, Department = "Internal Medicine", Specialty = "Cardiology", LicenseNumber = "MD-RW-2018-0442", LicenseExpiryDate = DateTime.UtcNow.AddYears(2), PhoneNumber = "+250 788 111 222", Email = "r.mugabe@smartcare.org", IsOnDuty = true },
        new StaffProfileDto { Id = Guid.NewGuid(), UserName = "nurse", FullName = "Grace Mutoni, RN", Role = AppRoles.Nurse, Department = "Emergency / Triage", Specialty = "Critical Care Nursing", LicenseNumber = "RN-RW-2020-0891", LicenseExpiryDate = DateTime.UtcNow.AddYears(1), PhoneNumber = "+250 788 333 444", Email = "g.mutoni@smartcare.org", IsOnDuty = true },
        new StaffProfileDto { Id = Guid.NewGuid(), UserName = "pharmacist", FullName = "Patrick Habineza, PharmD", Role = AppRoles.Pharmacist, Department = "Pharmacy", Specialty = "Clinical Pharmacy", LicenseNumber = "RPH-RW-2019-0128", LicenseExpiryDate = DateTime.UtcNow.AddYears(3), PhoneNumber = "+250 788 555 666", Email = "p.habineza@smartcare.org", IsOnDuty = true },
        new StaffProfileDto { Id = Guid.NewGuid(), UserName = "lab", FullName = "Diane Uwamahoro, MLS", Role = AppRoles.LabTechnician, Department = "Pathology & Laboratory", Specialty = "Clinical Biochemistry", LicenseNumber = "MLS-RW-2021-0312", LicenseExpiryDate = DateTime.UtcNow.AddYears(2), PhoneNumber = "+250 788 777 888", Email = "d.uwamahoro@smartcare.org", IsOnDuty = true },
        new StaffProfileDto { Id = Guid.NewGuid(), UserName = "receptionist", FullName = "Aline Umutoni", Role = AppRoles.Receptionist, Department = "Outpatient Administration", Specialty = "Patient Flow & Triage", LicenseNumber = "ADM-RW-2022-0051", LicenseExpiryDate = DateTime.UtcNow.AddYears(4), PhoneNumber = "+250 788 999 000", Email = "a.umutoni@smartcare.org", IsOnDuty = true },
        new StaffProfileDto { Id = Guid.NewGuid(), UserName = "billing", FullName = "Emmanuel Nkurunziza", Role = AppRoles.BillingStaff, Department = "Finance & Revenue", Specialty = "Hospital Accounting", LicenseNumber = "ACC-RW-2017-0982", LicenseExpiryDate = DateTime.UtcNow.AddYears(3), PhoneNumber = "+250 788 123 456", Email = "e.nkurunziza@smartcare.org", IsOnDuty = true },
        new StaffProfileDto { Id = Guid.NewGuid(), UserName = "admin", FullName = "Hospital Administrator", Role = AppRoles.HospitalAdmin, Department = "Executive Operations", Specialty = "Healthcare Management", LicenseNumber = "HA-RW-2015-0012", LicenseExpiryDate = DateTime.UtcNow.AddYears(5), PhoneNumber = "+250 788 000 111", Email = "admin@smartcare.org", IsOnDuty = true },
        new StaffProfileDto { Id = Guid.NewGuid(), UserName = "superadmin", FullName = "Chief Technical Officer", Role = AppRoles.SuperAdmin, Department = "Information Technology", Specialty = "Hospital Information Systems", LicenseNumber = "IT-RW-2016-0001", LicenseExpiryDate = DateTime.UtcNow.AddYears(10), PhoneNumber = "+250 788 999 888", Email = "superadmin@smartcare.org", IsOnDuty = true }
    ];

    public Task<IReadOnlyList<StaffProfileDto>> GetStaffProfilesAsync(string? department = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(department))
            return Task.FromResult<IReadOnlyList<StaffProfileDto>>(_staff);

        var filtered = _staff.Where(s => s.Department.Contains(department, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<StaffProfileDto>>(filtered);
    }

    public Task<IReadOnlyList<DutyRosterDto>> GetRostersAsync(DateTime? period = null, CancellationToken cancellationToken = default)
    {
        var start = period?.Date ?? DateTime.UtcNow.Date;
        var end = start.AddDays(7);

        var rosters = new List<DutyRosterDto>
        {
            new DutyRosterDto
            {
                Id = Guid.NewGuid(),
                Department = "Emergency & Triage",
                PeriodStart = start,
                PeriodEnd = end,
                Assignments =
                [
                    new ShiftAssignmentDto { StaffId = _staff[1].Id, StaffName = _staff[1].FullName, Role = _staff[1].Role, ShiftType = "Morning (07:00-15:00)", Date = start, Status = "Confirmed" },
                    new ShiftAssignmentDto { StaffId = _staff[0].Id, StaffName = _staff[0].FullName, Role = _staff[0].Role, ShiftType = "On-Call", Date = start, Status = "Confirmed" }
                ]
            },
            new DutyRosterDto
            {
                Id = Guid.NewGuid(),
                Department = "Laboratory & Pharmacy",
                PeriodStart = start,
                PeriodEnd = end,
                Assignments =
                [
                    new ShiftAssignmentDto { StaffId = _staff[2].Id, StaffName = _staff[2].FullName, Role = _staff[2].Role, ShiftType = "Morning (07:00-15:00)", Date = start, Status = "Confirmed" },
                    new ShiftAssignmentDto { StaffId = _staff[3].Id, StaffName = _staff[3].FullName, Role = _staff[3].Role, ShiftType = "Morning (07:00-15:00)", Date = start, Status = "Confirmed" }
                ]
            }
        };

        return Task.FromResult<IReadOnlyList<DutyRosterDto>>(rosters);
    }

    public Task<byte[]> ExportAttendanceCsvAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Staff Name,Role,Department,Shift Date,Shift Type,Clock In,Clock Out,Hours Worked,Compliance Status");

        foreach (var s in _staff)
        {
            for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            {
                sb.AppendLine($"\"{s.FullName}\",\"{s.Role}\",\"{s.Department}\",{d:yyyy-MM-dd},\"Morning (07:00-15:00)\",\"06:55\",\"15:05\",8.0,\"Compliant\"");
            }
        }

        return Task.FromResult(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray());
    }
}
