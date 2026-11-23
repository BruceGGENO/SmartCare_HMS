using System.Security.Claims;
using SmartCare.Application.Common;

namespace SmartCare.Web.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;
    public CurrentUser(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public Guid? StaffId => Guid.TryParse(Principal?.FindFirstValue("staff_id"), out var id) ? id : null;
    public string? UserName => Principal?.Identity?.Name;
    public string? IpAddress => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
