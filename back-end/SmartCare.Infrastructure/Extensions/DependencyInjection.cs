using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
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

        return services;
    }
}
