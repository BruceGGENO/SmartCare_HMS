namespace SmartCare.Application.Contracts;

/// <summary>
/// Agreed between Seat D (pharmacy, lab) and Seat E (billing, ledger).
/// Raised whenever a service has really been delivered: medicine dispensed, test result released.
/// Seat E turns it into an invoice line and a Usage entry in the ledger.
/// UnitValue is the price of ONE unit; the total is Quantity x UnitValue.
/// </summary>
public record ServiceCompleted(
    Guid PatientId,
    Guid? EncounterId,
    Guid? PriceItemId,
    Guid? DoctorId,
    string SourceModule,       // "Pharmacy" or "Lab"
    string Description,
    int Quantity,
    decimal UnitValue,
    DateTime OccurredAtUtc);

public interface IServiceCompletedPublisher
{
    /// <summary>Adds the event to the current unit of work. It is saved by the caller's SaveChanges, so the service and its event are saved together or not at all.</summary>
    void Publish(ServiceCompleted evt);
}

public record ClearanceCheckResult(bool CanClear, string? Reason);

/// <summary>
/// Seat C (discharge) asks each department whether it can clear a patient.
/// Seat D provides the "Pharmacy" and "Laboratory" implementations.
/// </summary>
public interface IDepartmentClearanceCheck
{
    string DepartmentName { get; }
    Task<ClearanceCheckResult> CheckAsync(Guid encounterId, CancellationToken ct = default);
}

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string originalFileName, long length, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default);
}
