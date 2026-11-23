using SmartCare.Application.Contracts;
using SmartCare.Domain.Entities;
using SmartCare.Infrastructure.Persistence;

namespace SmartCare.Infrastructure.Services;

/// <summary>
/// Writes a ServiceEvent row in the caller's unit of work. Seat E reads these rows to create invoice lines
/// and Usage entries in the ledger. Nothing is saved here: the caller's SaveChanges saves the service and its event together.
/// </summary>
public class ServiceEventPublisher : IServiceCompletedPublisher
{
    private readonly AppDbContext _db;
    public ServiceEventPublisher(AppDbContext db) => _db = db;

    public void Publish(ServiceCompleted evt)
    {
        _db.ServiceEvents.Add(new ServiceEvent
        {
            PatientId = evt.PatientId,
            EncounterId = evt.EncounterId,
            PriceItemId = evt.PriceItemId,
            DoctorId = evt.DoctorId,
            SourceModule = evt.SourceModule,
            Description = evt.Description,
            Quantity = evt.Quantity,
            UnitValue = evt.UnitValue,
            OccurredAtUtc = evt.OccurredAtUtc
        });
    }
}
