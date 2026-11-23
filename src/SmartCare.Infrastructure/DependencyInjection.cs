using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartCare.Application.Common;
using SmartCare.Application.Contracts;
using SmartCare.Infrastructure.Identity;
using SmartCare.Infrastructure.Persistence;
using SmartCare.Infrastructure.Services;

namespace SmartCare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IServiceCompletedPublisher, ServiceEventPublisher>();
        services.AddSingleton(new FileStorageOptions
        {
            RootPath = config["FileStorage:RootPath"] ?? "App_Data/uploads",
            MaxBytes = config.GetValue<long?>("FileStorage:MaxBytes") ?? 10 * 1024 * 1024
        });
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(o =>
            {
                // NFR-SEC-006
                o.Password.RequiredLength = 12;
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                o.Lockout.AllowedForNewUsers = true;
                o.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
