using FluentValidation;
using Scalar.AspNetCore;
using SmartCare.API.Extensions;
using SmartCare.API.Security;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Application.Validators;
using SmartCare.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

builder.Services.AddSmartCareInfrastructure(builder.Configuration);
builder.Services.AddSmartCareAuthentication(builder.Configuration);
builder.Services.AddSmartCareCors(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<IAuthService>().EnsureDemoUsersAsync();
    await scope.ServiceProvider.GetRequiredService<IAppointmentService>().EnsureDemoDataAsync();
}

app.UseHttpsRedirection();
app.UseCors(AuthenticationExtensions.CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
