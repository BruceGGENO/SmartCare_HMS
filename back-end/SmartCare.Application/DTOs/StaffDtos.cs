namespace SmartCare.Application.DTOs;

public sealed class StaffProfileDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public DateTime? LicenseExpiryDate { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsOnDuty { get; set; } = true;
}

public sealed class DutyRosterDto
{
    public Guid Id { get; set; }
    public string Department { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<ShiftAssignmentDto> Assignments { get; set; } = [];
}

public sealed class ShiftAssignmentDto
{
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string ShiftType { get; set; } = "Morning"; // Morning (07:00-15:00), Evening (15:00-23:00), Night (23:00-07:00), On-Call
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Confirmed";
}

public sealed class AttendanceExportItem
{
    public string StaffName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ShiftType { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal HoursWorked { get; set; }
    public string ClockIn { get; set; } = string.Empty;
    public string ClockOut { get; set; } = string.Empty;
}
