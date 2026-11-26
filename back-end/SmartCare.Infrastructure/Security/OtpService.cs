using System.Data.Common;
using System.Security.Cryptography;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Domain.Constants;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Security;

public sealed class OtpService : IOtpService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);

    private readonly ISqlExecutor _sql;
    private readonly IAuthService _auth;
    private readonly IPasswordHasher _hasher;

    public OtpService(ISqlExecutor sql, IAuthService auth, IPasswordHasher hasher)
    {
        _sql = sql;
        _auth = auth;
        _hasher = hasher;
    }

    public async Task<string?> RequestLoginOtpAsync(string userName, CancellationToken cancellationToken = default)
    {
        var user = await _auth.FindByUserNameAsync(userName, cancellationToken);
        if (user is null || !user.IsActive)
            return null;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var (hash, salt) = _hasher.Hash(code);
        var expires = DateTimeOffset.UtcNow.Add(OtpLifetime);

        await _sql.ExecuteNonQueryAsync(
            "dbo.sp_InsertOtp",
            new Dictionary<string, object?>
            {
                ["OtpId"] = Guid.NewGuid(),
                ["UserId"] = user.UserId,
                ["CodeHash"] = hash,
                ["CodeSalt"] = salt,
                ["Purpose"] = "Login",
                ["ExpiresAtUtc"] = expires
            },
            cancellationToken);

        await _sql.ExecuteNonQueryAsync(
            "dbo.sp_InsertOutbox",
            new Dictionary<string, object?>
            {
                ["Channel"] = "SMS",
                ["Recipient"] = user.UserName,
                ["Subject"] = "SmartCare login code",
                ["Body"] = $"Your SmartCare login code is {code}. It expires in 10 minutes."
            },
            cancellationToken);

        return code;
    }

    public async Task<LoginResult> LoginWithOtpAsync(string userName, string code, CancellationToken cancellationToken = default)
    {
        var user = await _auth.FindByUserNameAsync(userName, cancellationToken);
        if (user is null || !user.IsActive)
            return LoginResult.Fail("Invalid username or code.");

        if (!user.Roles.Contains(AppRoles.Patient, StringComparer.OrdinalIgnoreCase))
            return LoginResult.Fail("OTP login is for patient accounts only.");

        var rows = await _sql.QueryAsync(
            "dbo.sp_GetLatestOpenOtp",
            MapOtp,
            new Dictionary<string, object?> { ["UserId"] = user.UserId },
            cancellationToken);

        if (rows.Count == 0)
            return LoginResult.Fail("Invalid username or code.");

        var otp = rows[0];
        if (otp.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            return LoginResult.Fail("That code has expired. Request a new one.");

        if (!_hasher.Verify(code.Trim(), otp.CodeHash, otp.CodeSalt))
            return LoginResult.Fail("Invalid username or code.");

        await _sql.ExecuteNonQueryAsync(
            "dbo.sp_ConsumeOtp",
            new Dictionary<string, object?> { ["OtpId"] = otp.OtpId },
            cancellationToken);

        return LoginResult.Success(user);
    }

    public Task<IReadOnlyList<OutboxMessage>> ListRecentAsync(CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_ListOutbox", MapOutbox, cancellationToken: cancellationToken);

    private static OtpRow MapOtp(DbDataReader record) => new(
        record.Get<Guid>("OtpId"),
        record.Get<string>("CodeHash"),
        record.Get<string>("CodeSalt"),
        record.GetValue(record.GetOrdinal("ExpiresAtUtc")) is DateTimeOffset dto
            ? dto
            : new DateTimeOffset(DateTime.SpecifyKind(record.Get<DateTime>("ExpiresAtUtc"), DateTimeKind.Utc)));

    private static OutboxMessage MapOutbox(DbDataReader record) => new()
    {
        OutboxId = Convert.ToInt64(record.GetValue(record.GetOrdinal("OutboxId"))),
        Channel = record.Get<string>("Channel"),
        Recipient = record.Get<string>("Recipient"),
        Subject = record.Get<string>("Subject"),
        Body = record.Get<string>("Body"),
        CreatedAtUtc = record.GetValue(record.GetOrdinal("CreatedAtUtc")) is DateTimeOffset created
            ? created
            : DateTimeOffset.UtcNow,
        Status = record.Get<string>("Status")
    };

    private sealed record OtpRow(Guid OtpId, string CodeHash, string CodeSalt, DateTimeOffset ExpiresAtUtc);
}
