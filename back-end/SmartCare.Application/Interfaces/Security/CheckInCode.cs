namespace SmartCare.Application.Interfaces.Security;

public sealed class CheckInCode
{
    public string Code { get; init; } = string.Empty;

    public string SvgMarkup { get; init; } = string.Empty;
}
