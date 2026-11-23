using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Infrastructure.Identity;

namespace SmartCare.Web.Pages.Account;

public class LogoutModel : AppPageModel
{
    private readonly SignInManager<ApplicationUser> _signIn;
    public LogoutModel(SignInManager<ApplicationUser> signIn) => _signIn = signIn;

    public IActionResult OnGet() => RedirectToPage("/Account/Login");

    public async Task<IActionResult> OnPostAsync()
    {
        await _signIn.SignOutAsync();
        return RedirectToPage("/Account/Login");
    }
}
