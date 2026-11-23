using SmartCare.Infrastructure.Persistence;

namespace SmartCare.Web.Services;

/// <summary>
/// Runs at application start (not at design time), so "dotnet ef" can build the host before any table exists.
/// Applies pending migrations, then seeds roles, departments and (in development) sample data.
/// </summary>
public class DatabaseSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;

    public DatabaseSeeder(IServiceProvider services, IConfiguration config)
    {
        _services = services;
        _config = config;
    }

    public Task StartAsync(CancellationToken cancellationToken) => DbInitializer.SeedAsync(_services, _config);
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
