using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;
using SmartCare.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("SmartCare")
    ?? throw new InvalidOperationException("Connection string was not found.");

// Register database executor and billing services
builder.Services.AddScoped<ISqlExecutor>(_ => new SqlExecutor(connectionString));
builder.Services.AddScoped<IPaymentGateway, PaymentGatewayStub>();
builder.Services.AddScoped<IBillingService, BillingService>();

var app = builder.Build();

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
