using Microsoft.Extensions.DependencyInjection;
using SmartCare.Application.Common;
using SmartCare.Application.Contracts;
using SmartCare.Application.Discharge;
using SmartCare.Application.Lab;
using SmartCare.Application.Pharmacy;
using SmartCare.Application.WaitingRoom;

namespace SmartCare.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ClinicalLookup>();

        // Seat D: pharmacy and lab
        services.AddScoped<PharmacyService>();
        services.AddScoped<LabService>();
        services.AddScoped<IDepartmentClearanceCheck, PharmacyClearanceCheck>();
        services.AddScoped<IDepartmentClearanceCheck, LabClearanceCheck>();

        // Seat E (Elvin): discharge turnaround + waiting room analytics
        services.AddScoped<DischargeService>();
        services.AddScoped<WaitingRoomService>();

        return services;
    }
}
