using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartCare.Application.Common;
using SmartCare.Domain.Common;
using SmartCare.Domain.Entities;
using SmartCare.Infrastructure.Identity;

namespace SmartCare.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IAppDbContext
{
    private readonly ICurrentUser? _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser? currentUser = null) : base(options)
    {
        _currentUser = currentUser;
    }

    // Patient & EHR
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<InsuranceCoverage> InsuranceCoverages => Set<InsuranceCoverage>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<MedicalHistoryEntry> MedicalHistoryEntries => Set<MedicalHistoryEntry>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<Admission> Admissions => Set<Admission>();
    public DbSet<Vitals> Vitals => Set<Vitals>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<Consent> Consents => Set<Consent>();

    // Staff & scheduling
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<DutyRoster> DutyRosters => Set<DutyRoster>();
    public DbSet<RosterAssignment> RosterAssignments => Set<RosterAssignment>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();
    public DbSet<DoctorTimeOff> DoctorTimeOffs => Set<DoctorTimeOff>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentStatusHistory> AppointmentStatusHistories => Set<AppointmentStatusHistory>();
    public DbSet<MessageLog> MessageLogs => Set<MessageLog>();

    // Facility
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Bed> Beds => Set<Bed>();

    // Billing
    public DbSet<PriceItem> PriceItems => Set<PriceItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InsuranceClaim> InsuranceClaims => Set<InsuranceClaim>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PackagePurchase> PackagePurchases => Set<PackagePurchase>();

    // Pharmacy
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<DispenseRecord> DispenseRecords => Set<DispenseRecord>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    // Lab & radiology
    public DbSet<TestCatalogItem> TestCatalogItems => Set<TestCatalogItem>();
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<Sample> Samples => Set<Sample>();
    public DbSet<SampleStatusHistory> SampleStatusHistories => Set<SampleStatusHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<ImagingStudy> ImagingStudies => Set<ImagingStudy>();

    // Differentiator modules
    public DbSet<BedTransferRequest> BedTransferRequests => Set<BedTransferRequest>();
    public DbSet<Discharge> Discharges => Set<Discharge>();
    public DbSet<DischargeClearanceItem> DischargeClearanceItems => Set<DischargeClearanceItem>();
    public DbSet<DischargeAddendum> DischargeAddenda => Set<DischargeAddendum>();
    public DbSet<PatientJourneyEvent> PatientJourneyEvents => Set<PatientJourneyEvent>();
    public DbSet<OccupancySnapshot> OccupancySnapshots => Set<OccupancySnapshot>();
    public DbSet<OccupancyForecast> OccupancyForecasts => Set<OccupancyForecast>();
    public DbSet<ServiceEvent> ServiceEvents => Set<ServiceEvent>();
    public DbSet<RevenueUsageLedger> LedgerEntries => Set<RevenueUsageLedger>();

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().HaveMaxLength(256);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        foreach (var et in b.Model.GetEntityTypes().ToList())
        {
            // Long free-text columns
            foreach (var p in et.GetProperties())
            {
                // Enums are stored as readable text (e.g. "Cleared"), max 40 characters
                var clrType = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
                if (clrType.IsEnum)
                {
                    p.SetProviderClrType(typeof(string));
                    p.SetMaxLength(40);
                }
                if (p.PropertyInfo?.GetCustomAttribute<LongTextAttribute>() is not null)
                    p.SetMaxLength(null);
            }

            // Never cascade-delete clinical or financial data
            if (et.ClrType.Namespace?.StartsWith("SmartCare.Domain") == true)
                foreach (var fk in et.GetForeignKeys())
                    fk.DeleteBehavior = DeleteBehavior.Restrict;

            // Optimistic concurrency (beds, appointment slots)
            if (typeof(IConcurrent).IsAssignableFrom(et.ClrType))
                b.Entity(et.ClrType).Property(nameof(IConcurrent.RowVersion)).IsRowVersion();
        }

        b.Entity<Patient>(e =>
        {
            e.HasIndex(x => x.Mrn).IsUnique();
            e.HasIndex(x => new { x.LastName, x.FirstName, x.DateOfBirth, x.Phone });   // duplicate detection
        });
        b.Entity<Bed>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Appointment>(e =>
        {
            // one active booking per doctor per slot; cancelled slots are released
            e.HasIndex(x => new { x.DoctorId, x.StartUtc }).IsUnique().HasFilter("[Status] <> 'Cancelled'");
            e.HasIndex(x => new { x.PatientId, x.StartUtc });
        });
        b.Entity<Invoice>().HasIndex(x => x.InvoiceNumber).IsUnique();
        b.Entity<Payment>().HasIndex(x => x.GatewayTransactionId).IsUnique().HasFilter("[GatewayTransactionId] IS NOT NULL");
        b.Entity<InsuranceClaim>().HasIndex(x => x.InvoiceId).IsUnique();
        b.Entity<PriceItem>().HasIndex(x => new { x.Code, x.EffectiveFrom }).IsUnique();
        b.Entity<Drug>().HasIndex(x => x.Code).IsUnique();
        b.Entity<StockBatch>().HasIndex(x => new { x.DrugId, x.StoreName, x.BatchNumber }).IsUnique();
        b.Entity<TestCatalogItem>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Sample>().HasIndex(x => x.Barcode).IsUnique();
        b.Entity<Discharge>().HasIndex(x => x.AdmissionId).IsUnique();
        b.Entity<PatientJourneyEvent>().HasIndex(x => new { x.Station, x.OccurredAtUtc });
        b.Entity<OccupancySnapshot>().HasIndex(x => new { x.WardId, x.SnapshotDate }).IsUnique();
        b.Entity<OccupancyForecast>().HasIndex(x => new { x.WardId, x.ForecastDate });
        b.Entity<RevenueUsageLedger>(e =>
        {
            e.HasIndex(x => x.OccurredAtUtc);
            e.HasIndex(x => x.PackagePurchaseId);
        });
        b.Entity<Notification>().HasIndex(x => new { x.UserId, x.IsRead });
        b.Entity<AuditLog>(e =>
        {
            e.HasIndex(x => new { x.PatientId, x.OccurredAtUtc });
            e.HasIndex(x => new { x.UserId, x.OccurredAtUtc });
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Stamps UpdatedAtUtc, blocks edits to append-only tables, and writes audit rows for PHI changes.</summary>
    private void PrepareChanges()
    {
        var now = DateTime.UtcNow;
        var audits = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries().ToList())
        {
            var changed = entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

            if ((entry.Entity is AuditLog or RevenueUsageLedger) &&
                entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException(
                    $"{entry.Entity.GetType().Name} is append-only. Add a new entry instead of changing or deleting one.");

            if (entry.Entity is BaseEntity be && entry.State == EntityState.Modified)
                be.UpdatedAtUtc = now;

            if (changed && entry.Entity is IPhiEntity)
                audits.Add(BuildAudit(entry, now));
        }

        if (audits.Count > 0) AuditLogs.AddRange(audits);
    }

    private AuditLog BuildAudit(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, DateTime now)
    {
        Guid? patientId = entry.Entity is Patient p
            ? p.Id
            : entry.Properties.FirstOrDefault(x => x.Metadata.Name == "PatientId")?.CurrentValue as Guid?;

        return new AuditLog
        {
            UserId = _currentUser?.UserId,
            UserName = _currentUser?.UserName,
            IpAddress = _currentUser?.IpAddress,
            Action = entry.State switch
            {
                EntityState.Added => AuditAction.Create,
                EntityState.Deleted => AuditAction.Delete,
                _ => AuditAction.Update
            },
            EntityType = entry.Metadata.ClrType.Name,
            EntityId = entry.Property("Id").CurrentValue?.ToString(),
            PatientId = patientId,
            // field names only, never values, so the audit log cannot leak PHI
            ChangedFields = entry.State == EntityState.Modified
                ? string.Join(",", entry.Properties.Where(x => x.IsModified).Select(x => x.Metadata.Name))
                : null,
            OccurredAtUtc = now
        };
    }
}
