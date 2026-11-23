using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using SmartCare.Application;
using SmartCare.Application.Common;
using SmartCare.Infrastructure;
using SmartCare.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddHostedService<DatabaseSeeder>();

// NFR-SEC-005: idle timeout 15 minutes, absolute limit 12 hours
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromMinutes(15);
    o.SlidingExpiration = true;
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Events.OnValidatePrincipal = async ctx =>
    {
        await SecurityStampValidator.ValidatePrincipalAsync(ctx);   // keep Identity's own check
        if (ctx.Principal is null) return;

        var started = ctx.Principal.FindFirst("auth_time")?.Value;
        if (!long.TryParse(started, out var seconds) ||
            DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(seconds) > TimeSpan.FromHours(12))
        {
            ctx.RejectPrincipal();
            await ctx.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddLocalization();
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/");                       // everything needs a login ...
    o.Conventions.AllowAnonymousToFolder("/Account");         // ... except the account pages
    o.Conventions.AllowAnonymousToPage("/Error");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures("en", "fr", "rw")
    .AddSupportedUICultures("en", "fr", "rw"));
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
