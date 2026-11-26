namespace SmartCare.Application.DTOs.Responses;

public sealed class OtpRequestResponse
{
    public string Message { get; init; } = string.Empty;

    /// <summary>Only populated in Development so the team can demo without SMS.</summary>
    public string? DemoCode { get; init; }
}
