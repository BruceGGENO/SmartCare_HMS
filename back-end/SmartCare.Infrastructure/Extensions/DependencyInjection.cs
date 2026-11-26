using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Auditing;
using SmartCare.Infrastructure.Configurations;
using SmartCare.Infrastructure.Data;
using SmartCare.Infrastructure.Security;
using SmartCare.Infrastructure.Services;

namespace SmartCare.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddSmartCareInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ICheckInCodeGenerator, CheckInCodeGenerator>();
        services.AddScoped<ISqlExecutor>(_ => new SqlExecutor(connectionString));
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IPaymentGateway, PaymentGatewayStub>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IRevenueService, RevenueService>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        return services;
    }
}
