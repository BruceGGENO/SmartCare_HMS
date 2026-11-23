using Microsoft.AspNetCore.Identity;

namespace SmartCare.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? StaffId { get; set; }      // set for staff accounts
    public Guid? PatientId { get; set; }    // set for patient-portal accounts (one account : one patient record)
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
