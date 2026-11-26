namespace SmartCare.Application.Interfaces.Security;

public sealed class AuthUser
{
    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTimeOffset? LockoutEndUtc { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = [];

    public IReadOnlyList<string> Permissions { get; set; } = [];
}
