using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCare.Application.Common;

namespace SmartCare.Web.Pages;

/// <summary>Base for every page: runs an action and turns a broken hospital rule into a red message instead of a crash.</summary>
public abstract class AppPageModel : PageModel
{
    protected async Task<bool> TryAsync(Func<Task> action, string? success = null)
    {
        try
        {
            await action();
            if (success is not null) TempData["Success"] = success;
            return true;
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = ex.Message;
            return false;
        }
    }
}
