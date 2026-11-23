using SmartCare.Application.Common;

namespace SmartCare.Web.Pages.Account;

/// <summary>FR-EHR-006: a refused access ends here (403) and is written to the audit log.</summary>
public class AccessDeniedModel : AppPageModel
{
    private readonly IAuditService _audit;
    public AccessDeniedModel(IAuditService audit) => _audit = audit;

    public async Task OnGetAsync(string? returnUrl)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        await _audit.LogDeniedAsync("Page", returnUrl);
    }
}
