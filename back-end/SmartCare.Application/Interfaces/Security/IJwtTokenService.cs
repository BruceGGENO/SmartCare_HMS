namespace SmartCare.Application.Interfaces.Security;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(AuthUser user);
}
