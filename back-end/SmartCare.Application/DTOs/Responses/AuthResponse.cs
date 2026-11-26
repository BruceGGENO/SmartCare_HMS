namespace SmartCare.Application.DTOs.Responses;

public sealed class AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string TokenType { get; init; } = "Bearer";

    public DateTimeOffset ExpiresAtUtc { get; init; }

    public UserSummaryResponse User { get; init; } = new();
}
