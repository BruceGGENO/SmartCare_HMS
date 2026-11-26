using FluentValidation;
using Scalar.AspNetCore;
using SmartCare.API.Extensions;
using SmartCare.API.Middleware;
using SmartCare.API.Security;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Application.Validators;
using SmartCare.Infrastructure.Data;
using SmartCare.Infrastructure.Extensions;
using SmartCare.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

builder.Services.AddSmartCareInfrastructure(builder.Configuration);
builder.Services.AddSmartCareAuthentication(builder.Configuration);
builder.Services.AddSmartCareCors(builder.Configuration);

var uploadsRoot = Path.Combine(
    builder.Environment.ContentRootPath,
    builder.Configuration.GetValue("FileStorage:RootPath", "App_Data/uploads")!);
var maxFileBytes = builder.Configuration.GetValue("FileStorage:MaxFileBytes", 5 * 1024 * 1024);
Directory.CreateDirectory(uploadsRoot);
builder.Services.AddScoped<IFileStorage>(sp =>
    new LocalFileStorage(sp.GetRequiredService<ISqlExecutor>(), uploadsRoot, maxFileBytes));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<IAuthService>().EnsureDemoUsersAsync();
    await scope.ServiceProvider.GetRequiredService<IAppointmentService>().EnsureDemoDataAsync();
    await scope.ServiceProvider.GetRequiredService<IRevenueService>().EnsureDemoDataAsync();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHttpsRedirection();
app.UseCors(AuthenticationExtensions.CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();