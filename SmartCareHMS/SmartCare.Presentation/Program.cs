using Microsoft.AspNetCore.Authentication.Cookies;
using SmartCare.Application.Common;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Auditing;
using SmartCare.Infrastructure.Authentication;
using SmartCare.Infrastructure.Data;
using SmartCare.Infrastructure.FileStorage;
using SmartCare.Infrastructure.Services;
using SmartCare.Presentation.Middleware;
using SmartCare.Presentation.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("SmartCare")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddScoped<ISqlExecutor>(_ => new SqlExecutor(connectionString));
builder.Services.AddScoped<IPaymentGateway, PaymentGatewayStub>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();

var uploadsRoot = Path.Combine(
    builder.Environment.ContentRootPath,
    builder.Configuration.GetValue("FileStorage:RootPath", "App_Data/uploads")!);
var maxFileBytes = builder.Configuration.GetValue("FileStorage:MaxFileBytes", 5 * 1024 * 1024);
Directory.CreateDirectory(uploadsRoot);
builder.Services.AddScoped<IFileStorage>(sp =>
    new LocalFileStorage(sp.GetRequiredService<ISqlExecutor>(), uploadsRoot, maxFileBytes));

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ICheckInCodeGenerator, CheckInCodeGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Forbidden";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "SmartCare.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var role in AppRoles.All)
        options.AddPolicy(role, policy => policy.RequireRole(role));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<IAuthService>()
        .EnsureDemoUsersAsync();
}

app.UseMiddleware<CorrelationIdMiddleware>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
