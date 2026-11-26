using System.Data.Common;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Domain.Constants;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Security;

public sealed class AuthService : IAuthService
{
    public const string DemoPassword = "Demo@12345";
    private const int MaxFailedLogins = 5;
    private static readonly TimeSpan LockoutFor = TimeSpan.FromMinutes(15);

    private readonly ISqlExecutor _sql;
    private readonly IPasswordHasher _hasher;

    public AuthService(ISqlExecutor sql, IPasswordHasher hasher)
    {
        _sql = sql;
        _hasher = hasher;
    }

    public async Task<LoginResult> LoginAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        var user = await FindByUserNameAsync(userName, cancellationToken);
        if (user is null || !user.IsActive)
            return LoginResult.Fail("Invalid username or password.");

        if (user.LockoutEndUtc is { } until && until > DateTimeOffset.UtcNow)
            return LoginResult.Locked();

        if (!_hasher.Verify(password, user.PasswordHash, user.PasswordSalt))
        {
            var fails = user.FailedLoginCount + 1;
            DateTimeOffset? lockout = fails >= MaxFailedLogins
                ? DateTimeOffset.UtcNow.Add(LockoutFor)
                : null;

            await _sql.ExecuteNonQueryAsync(
                "dbo.sp_UpdateLoginFailure",
                new Dictionary<string, object?>
                {
                    ["UserId"] = user.UserId,
                    ["FailedLoginCount"] = fails,
                    ["LockoutEndUtc"] = lockout
                },
                cancellationToken);

            return fails >= MaxFailedLogins
                ? LoginResult.Locked()
                : LoginResult.Fail("Invalid username or password.");
        }

        await _sql.ExecuteNonQueryAsync(
            "dbo.sp_ResetLoginFailure",
            new Dictionary<string, object?> { ["UserId"] = user.UserId },
            cancellationToken);

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        return LoginResult.Success(user);
    }

    public async Task<AuthUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return null;

        var rows = await _sql.QueryAsync(
            "dbo.sp_GetUserByUserName",
            MapUser,
            new Dictionary<string, object?> { ["UserName"] = userName.Trim() },
            cancellationToken);

        var user = rows.Count == 0 ? null : rows[0];
        if (user is null)
            return null;

        var roles = await _sql.QueryAsync(
            "dbo.sp_GetUserRoles",
            r => r.Get<string>("RoleName"),
            new Dictionary<string, object?> { ["UserId"] = user.UserId },
            cancellationToken);

        user.Roles = roles;
        return user;
    }

    public async Task EnsureDemoUsersAsync(CancellationToken cancellationToken = default)
    {
        var count = await _sql.ExecuteScalarAsync<int>("dbo.sp_CountUsers", cancellationToken: cancellationToken);
        if (count > 0)
            return;

        var demos = new (string UserName, string DisplayName, string Role)[]
        {
            ("superadmin", "Super Admin", AppRoles.SuperAdmin),
            ("admin", "Hospital Admin", AppRoles.HospitalAdmin),
            ("receptionist", "Receptionist", AppRoles.Receptionist),
            ("doctor", "Doctor", AppRoles.Doctor),
            ("nurse", "Nurse", AppRoles.Nurse),
            ("lab", "Lab Technician", AppRoles.LabTechnician),
            ("pharmacist", "Pharmacist", AppRoles.Pharmacist),
            ("billing", "Billing Staff", AppRoles.BillingStaff),
            ("patient", "Demo Patient", AppRoles.Patient)
        };

        foreach (var (userName, displayName, role) in demos)
        {
            var (hash, salt) = _hasher.Hash(DemoPassword);
            var userId = Guid.NewGuid();

            await _sql.ExecuteNonQueryAsync(
                "dbo.sp_InsertUser",
                new Dictionary<string, object?>
                {
                    ["UserId"] = userId,
                    ["UserName"] = userName,
                    ["DisplayName"] = displayName,
                    ["PasswordHash"] = hash,
                    ["PasswordSalt"] = salt
                },
                cancellationToken);

            await _sql.ExecuteNonQueryAsync(
                "dbo.sp_InsertUserRole",
                new Dictionary<string, object?>
                {
                    ["UserId"] = userId,
                    ["RoleName"] = role
                },
                cancellationToken);
        }
    }

    private static AuthUser MapUser(DbDataReader record) => new()
    {
        UserId = record.Get<Guid>("UserId"),
        UserName = record.Get<string>("UserName"),
        DisplayName = record.Get<string>("DisplayName"),
        IsActive = record.Get<bool>("IsActive"),
        FailedLoginCount = record.Get<int>("FailedLoginCount"),
        LockoutEndUtc = ReadNullableTime(record, "LockoutEndUtc"),
        PasswordHash = record.Get<string>("PasswordHash"),
        PasswordSalt = record.Get<string>("PasswordSalt")
    };

    private static DateTimeOffset? ReadNullableTime(DbDataReader record, string column)
    {
        var ordinal = record.GetOrdinal(column);
        if (record.IsDBNull(ordinal))
            return null;

        return record.GetValue(ordinal) switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => null
        };
    }
}
