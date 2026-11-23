using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Infrastructure.Identity;

namespace SmartCare.Web.Pages.Account;

public class LoginModel : AppPageModel
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;

    public LoginModel(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public string? ErrorMessage { get; private set; }

    public class InputModel
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();

        var user = await _users.FindByEmailAsync(Input.Email);
        if (user is null || !user.IsActive)
        {
            ErrorMessage = "Wrong email or password.";
            return Page();
        }

        var check = await _signIn.CheckPasswordSignInAsync(user, Input.Password, lockoutOnFailure: true);
        if (check.IsLockedOut)
        {
            ErrorMessage = "Too many failed attempts. This account is locked for 15 minutes.";
            return Page();
        }
        if (!check.Succeeded)
        {
            ErrorMessage = "Wrong email or password.";
            return Page();
        }

        // Extra claims: when the session started (12 hour limit) and the staff profile of this user
        var claims = new List<Claim> { new("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) };
        if (user.StaffId is not null) claims.Add(new Claim("staff_id", user.StaffId.Value.ToString()));
        await _signIn.SignInWithClaimsAsync(user, isPersistent: false, claims);

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
