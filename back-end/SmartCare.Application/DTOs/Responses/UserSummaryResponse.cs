namespace SmartCare.Application.DTOs.Responses;

public sealed class UserSummaryResponse
{
    public Guid UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];
}
