using SmartCare.Application.DTOs;

namespace SmartCare.Application.Interfaces.Services;

public interface IStaffService
{
    Task<IReadOnlyList<StaffProfileDto>> GetStaffProfilesAsync(string? department = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DutyRosterDto>> GetRostersAsync(DateTime? period = null, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAttendanceCsvAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
}
