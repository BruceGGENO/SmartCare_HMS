using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Common;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Presentation.Models;

namespace SmartCare.Presentation.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _auth;
        private readonly IOtpService _otp;
        private readonly IAuditLogger _audit;
        private readonly IWebHostEnvironment _env;

        public AccountController(
            IAuthService auth,
            IOtpService otp,
            IAuditLogger audit,
            IWebHostEnvironment env)
        {
            _auth = auth;
            _otp = otp;
            _audit = audit;
            _env = env;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(string.Empty, "Username and password are required.");
                return View(model);
            }

            var result = await _auth.LoginAsync(model.UserName, model.Password, cancellationToken);
            await WriteLoginAudit(result, model.UserName, "Password", cancellationToken);

            if (!result.Succeeded || result.User is null)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View(model);
            }

            await SignInAsync(result.User);
            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestOtp(LoginViewModel model, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(model.UserName))
            {
                ModelState.AddModelError(string.Empty, "Enter the patient username first.");
                return View("Login", model);
            }

            var code = await _otp.RequestLoginOtpAsync(model.UserName, cancellationToken);
            TempData["Info"] = "If that account exists, a login code was sent to the Outbox stub.";
            if (_env.IsDevelopment() && code is not null)
                TempData["DemoOtp"] = code;

            return View("Login", model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginOtp(LoginViewModel model, string? returnUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.OtpCode))
            {
                ModelState.AddModelError(string.Empty, "Username and OTP code are required.");
                return View("Login", model);
            }

            var result = await _otp.LoginWithOtpAsync(model.UserName, model.OtpCode, cancellationToken);
            await WriteLoginAudit(result, model.UserName, "OTP", cancellationToken);

            if (!result.Succeeded || result.User is null)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View("Login", model);
            }

            await SignInAsync(result.User);
            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Forbidden()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }

        [HttpGet]
        [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.HospitalAdmin)]
        public async Task<IActionResult> Outbox(CancellationToken cancellationToken)
        {
            var messages = await _otp.ListRecentAsync(cancellationToken);
            return View(messages);
        }

        private async Task SignInAsync(AuthUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.UserName),
                new("DisplayName", user.DisplayName)
            };

            foreach (var role in user.Roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
        }

        private async Task WriteLoginAudit(LoginResult result, string userName, string method, CancellationToken cancellationToken)
        {
            await _audit.LogAsync(new AuditEvent
            {
                Action = "Auth.Login",
                EntityType = "User",
                EntityId = result.User?.UserId.ToString() ?? userName,
                ActorUserId = result.User?.UserId.ToString() ?? userName,
                Outcome = result.Succeeded ? $"Success:{method}" : $"Failure:{method}"
            }, cancellationToken);
        }

        private IActionResult RedirectToLocal(string? returnUrl) =>
            !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction("Index", "Home");
    }
}
