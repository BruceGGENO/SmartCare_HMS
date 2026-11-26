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
        services.AddScoped<IAuditLogger, AuditLogger>();

        // MOD-01: Patient & EHR
        services.AddScoped<IPatientEhrService, PatientEhrService>();

        // MOD-03: Staff & Rosters
        services.AddScoped<IStaffService, StaffService>();

        // MOD-04: Billing & Payments
        services.AddScoped<IPaymentGateway, PaymentGatewayStub>();
        services.AddScoped<IBillingService, BillingService>();

        // MOD-05: Pharmacy & Inventory
        services.AddScoped<IPharmacyService, PharmacyService>();

        // MOD-06: Laboratory & Radiology
        services.AddScoped<ILabService, LabService>();

        // MOD-07: Bed Reservation & Transfer Orchestrator
        services.AddScoped<IBedOrchestratorService, BedOrchestratorService>();

        // MOD-08: Discharge Turnaround Tracker
        services.AddScoped<IDischargeService, DischargeService>();

        // MOD-09: Waiting Room Bottleneck Analytics
        services.AddScoped<IWaitingRoomService, WaitingRoomService>();

        // MOD-11: Predictive Bed Occupancy
        services.AddScoped<IOccupancyForecastingService, OccupancyForecastingService>();

        // MOD-12: Revenue vs Usage Accounting
        services.AddScoped<IRevenueAccountingService, RevenueAccountingService>();

        return services;
    }
}
