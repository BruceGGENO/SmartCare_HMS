namespace SmartCare.Application.DTOs.Requests;

public sealed class OtpLoginRequest
{
    public string UserName { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}
