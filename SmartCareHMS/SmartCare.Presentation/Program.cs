using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Auditing;
using SmartCare.Infrastructure.Data;
using SmartCare.Infrastructure.FileStorage;
using SmartCare.Presentation.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register SqlExecutor with connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddScoped<ISqlExecutor>(_ => new SqlExecutor(connectionString));
builder.Services.AddScoped<IAuditLogger, AuditLogger>();

var uploadsRoot = Path.Combine(
    builder.Environment.ContentRootPath,
    builder.Configuration.GetValue("FileStorage:RootPath", "App_Data/uploads")!);
var maxFileBytes = builder.Configuration.GetValue("FileStorage:MaxFileBytes", 5 * 1024 * 1024);
Directory.CreateDirectory(uploadsRoot);
builder.Services.AddScoped<IFileStorage>(sp =>
    new LocalFileStorage(sp.GetRequiredService<ISqlExecutor>(), uploadsRoot, maxFileBytes));

var app = builder.Build();

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
