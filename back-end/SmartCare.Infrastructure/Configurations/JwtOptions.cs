namespace SmartCare.Infrastructure.Configurations;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "SmartCare.API";

    public string Audience { get; set; } = "SmartCare.Clients";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 480;
}
