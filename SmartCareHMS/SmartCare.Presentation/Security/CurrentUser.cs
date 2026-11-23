using System.Security.Claims;
using SmartCare.Application.Interfaces.Security;

namespace SmartCare.Presentation.Security;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http)
    {
        _http = http;
    }

    private ClaimsPrincipal User => _http.HttpContext?.User ?? new ClaimsPrincipal();

    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

    public string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => User.Identity?.Name;

    public IReadOnlyCollection<string> Roles =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

    public bool IsInRole(string role) => User.IsInRole(role);
}
