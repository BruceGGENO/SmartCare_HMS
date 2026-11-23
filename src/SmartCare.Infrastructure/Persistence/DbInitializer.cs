using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartCare.Application.Security;
using SmartCare.Domain.Entities;
using SmartCare.Infrastructure.Identity;

namespace SmartCare.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        // Roles
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));

        // Departments (the clearance departments feed the discharge checklist)
        if (!await db.Departments.AnyAsync())
        {
            db.Departments.AddRange(
                new Department { Name = "Nursing", IsDischargeClearanceDepartment = true, IsMandatoryClearance = true },
                new Department { Name = "Pharmacy", IsDischargeClearanceDepartment = true, IsMandatoryClearance = true },
                new Department { Name = "Billing", IsDischargeClearanceDepartment = true, IsMandatoryClearance = true },
                new Department { Name = "Laboratory", IsDischargeClearanceDepartment = true, IsMandatoryClearance = false },
                new Department { Name = "Radiology" },
                new Department { Name = "Outpatient" },
                new Department { Name = "Emergency" },
                new Department { Name = "Administration" });
            await db.SaveChangesAsync();
        }

        // First Super Admin: email in appsettings, password from user-secrets or an environment variable
        var email = config["Seed:SuperAdminEmail"];
        var password = config["Seed:SuperAdminPassword"];
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
        {
            var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
            if (await users.FindByEmailAsync(email) is null)
            {
                var admin = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = "System Administrator"
                };
                var created = await users.CreateAsync(admin, password);
                if (!created.Succeeded)
                    throw new InvalidOperationException(
                        "Could not create the Super Admin: " + string.Join("; ", created.Errors.Select(e => e.Description)));
                await users.AddToRoleAsync(admin, Roles.SuperAdmin);
            }
        }

        // Development only: sample users, a test patient, drugs, tests and stock so every seat can try the system
        if (config.GetValue<bool>("Seed:DevData") && !string.IsNullOrWhiteSpace(config["Seed:DevPassword"]))
            await DevSeed.RunAsync(db, sp.GetRequiredService<UserManager<ApplicationUser>>(), config["Seed:DevPassword"]!);
    }
}
