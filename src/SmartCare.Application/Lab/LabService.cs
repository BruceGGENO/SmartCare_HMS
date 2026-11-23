using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SmartCare.Application.Common;
using SmartCare.Application.Contracts;
using SmartCare.Domain.Common;
using SmartCare.Domain.Entities;

namespace SmartCare.Application.Lab;

/// <summary>MOD-06 Laboratory and radiology (FR-LAB-001 to 007).</summary>
public class LabService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IServiceCompletedPublisher _events;
    private readonly IFileStorage _files;

    public LabService(IAppDbContext db, ICurrentUser user, IServiceCompletedPublisher events, IFileStorage files)
    {
        _db = db;
        _user = user;
        _events = events;
        _files = files;
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private Guid RequireUser() => _user.UserId ?? throw new BusinessRuleException("You must be signed in.");

    // ------------------------------------------------------------ test catalog

    public Task<List<TestCatalogItem>> GetTestsAsync(CancellationToken ct = default) =>
        _db.Set<TestCatalogItem>().AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.Kind).ThenBy(t => t.Name).ToListAsync(ct);

    public async Task<TestCatalogItem> CreateTestAsync(TestCatalogItem input, CancellationToken ct = default)
    {
        var code = (input.Code ?? string.Empty).Trim().ToUpperInvariant();
        var name = (input.Name ?? string.Empty).Trim();
        if (code.Length == 0 || name.Length == 0) throw new BusinessRuleException("Test code and name are required.");
        if (input.ReferenceLow > input.ReferenceHigh) throw new BusinessRuleException("The normal range is wrong: low is higher than high.");
        if (input.CriticalLow > input.CriticalHigh) throw new BusinessRuleException("The critical range is wrong: low is higher than high.");
        if (await _db.Set<TestCatalogItem>().AnyAsync(t => t.Code == code, ct)) throw new BusinessRuleException($"A test with code {code} already exists.");

        var test = new TestCatalogItem
        {
            Code = code, Name = name, Kind = input.Kind, Unit = Clean(input.Unit),
            ReferenceLow = input.ReferenceLow, ReferenceHigh = input.ReferenceHigh,
            CriticalLow = input.CriticalLow, CriticalHigh = input.CriticalHigh, PriceItemId = input.PriceItemId
        };
        _db.Set<TestCatalogItem>().Add(test);
        await _db.SaveChangesAsync(ct);
        return test;
    }

    // ------------------------------------------------------------ ordering (FR-LAB-001)

    public async Task<LabOrder> OrderTestAsync(Guid encounterId, Guid testId, CancellationToken ct = default)
    {
        var doctorId = _user.StaffId ?? throw new BusinessRuleException("Your account is not linked to a staff profile, so you cannot order tests.");
        var encounter = await _db.Set<Encounter>().FirstOrDefaultAsync(e => e.Id == encounterId, ct)
                        ?? throw new BusinessRuleException("Visit (encounter) not found.");
        if (encounter.EndedAtUtc is not null) throw new BusinessRuleException("This visit is already closed.");
        var test = await _db.Set<TestCatalogItem>().FirstOrDefaultAsync(t => t.Id == testId && t.IsActive, ct)
                   ?? throw new BusinessRuleException("Test not found.");

        var order = new LabOrder
        {
            PatientId = encounter.PatientId, EncounterId = encounter.Id,
            TestCatalogItemId = test.Id, OrderedByStaffId = doctorId
        };
        _db.Set<LabOrder>().Add(order);
        await _db.SaveChangesAsync(ct);
        return order;
    }

    public Task<List<LabOrder>> GetWorklistAsync(CancellationToken ct = default) =>
        _db.Set<LabOrder>().AsNoTracking()
           .Include(o => o.Patient).Include(o => o.Test).Include(o => o.Samples).Include(o => o.Results)
           .Where(o => o.Status == OrderStatus.Ordered || o.Status == OrderStatus.InProgress)
           .OrderBy(o => o.OrderedAtUtc).ToListAsync(ct);

    public Task<LabOrder?> GetOrderAsync(Guid orderId, CancellationToken ct = default) =>
        _db.Set<LabOrder>().AsNoTracking()
           .Include(o => o.Patient).Include(o => o.Test)
           .Include(o => o.Samples).ThenInclude(s => s.History)
           .Include(o => o.Results)
           .FirstOrDefaultAsync(o => o.Id == orderId, ct);

    // ------------------------------------------------------------ samples (FR-LAB-002)

    public async Task<Sample> CollectSampleAsync(Guid orderId, CancellationToken ct = default)
    {
        var userId = RequireUser();
        var order = await _db.Set<LabOrder>().Include(o => o.Test).Include(o => o.Samples).FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw new BusinessRuleException("Order not found.");
        if (order.Test.Kind != TestKind.Lab) throw new BusinessRuleException("Radiology orders do not use samples.");
        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled) throw new BusinessRuleException($"This order is {order.Status}.");
        if (order.Samples.Count > 0) throw new BusinessRuleException("A sample was already collected for this order.");

        var sample = new Sample
        {
            LabOrderId = order.Id, PatientId = order.PatientId,
            Barcode = await NewBarcodeAsync(ct), Status = SampleStatus.Collected, CollectedAtUtc = DateTime.UtcNow
        };
        sample.History.Add(new SampleStatusHistory { Status = SampleStatus.Collected, ByUserId = userId });
        _db.Set<Sample>().Add(sample);
        order.Status = OrderStatus.InProgress;
        await _db.SaveChangesAsync(ct);
        return sample;
    }

    /// <summary>Moves a sample one step: Collected, Received, In-process, Completed. Each step records who and when.</summary>
    public async Task AdvanceSampleAsync(Guid sampleId, CancellationToken ct = default)
    {
        var userId = RequireUser();
        var sample = await _db.Set<Sample>().FirstOrDefaultAsync(s => s.Id == sampleId, ct)
                     ?? throw new BusinessRuleException("Sample not found.");

        var next = sample.Status switch
        {
            SampleStatus.Collected => SampleStatus.Received,
            SampleStatus.Received => SampleStatus.InProcess,
            SampleStatus.InProcess => SampleStatus.Completed,
            _ => throw new BusinessRuleException("This sample is already completed.")
        };

        sample.Status = next;
        if (next == SampleStatus.Received) sample.ReceivedAtUtc = DateTime.UtcNow;
        if (next == SampleStatus.Completed) sample.CompletedAtUtc = DateTime.UtcNow;
        _db.Set<SampleStatusHistory>().Add(new SampleStatusHistory { SampleId = sample.Id, Status = next, ByUserId = userId });
        await _db.SaveChangesAsync(ct);
    }

    private async Task<string> NewBarcodeAsync(CancellationToken ct)
    {
        for (var i = 0; i < 10; i++)
        {
            var code = $"SMP-{DateTime.UtcNow:yyMMdd}-{RandomNumberGenerator.GetInt32(100000, 1000000)}";
            if (!await _db.Set<Sample>().AnyAsync(s => s.Barcode == code, ct)) return code;
        }
        throw new BusinessRuleException("Could not generate a unique sample barcode. Please try again.");
    }

    // ------------------------------------------------------------ results (FR-LAB-003, 005)

    /// <summary>
    /// A technician enters the result. Numbers are compared with the normal and critical ranges of the test.
    /// Text results (for example "Positive") are accepted but are not flagged automatically.
    /// The result stays hidden until a DIFFERENT technician verifies it.
    /// </summary>
    public async Task<LabResult> EnterResultAsync(Guid orderId, string value, CancellationToken ct = default)
    {
        var userId = RequireUser();
        if (string.IsNullOrWhiteSpace(value)) throw new BusinessRuleException("Enter the result value.");

        var order = await _db.Set<LabOrder>().Include(o => o.Test).Include(o => o.Samples).Include(o => o.Results)
                        .FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw new BusinessRuleException("Order not found.");
        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled) throw new BusinessRuleException($"This order is {order.Status}.");
        if (order.Results.Any(r => r.ReleasedAtUtc != null)) throw new BusinessRuleException("A result was already released for this order.");

        if (order.Test.Kind == TestKind.Lab && !order.Samples.Any(s => s.Status is SampleStatus.InProcess or SampleStatus.Completed))
            throw new BusinessRuleException("The sample must be In-process before a result can be entered.");

        var test = order.Test;
        var abnormal = false;
        var critical = false;
        if (decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            critical = (test.CriticalLow.HasValue && number < test.CriticalLow) || (test.CriticalHigh.HasValue && number > test.CriticalHigh);
            abnormal = critical
                       || (test.ReferenceLow.HasValue && number < test.ReferenceLow)
                       || (test.ReferenceHigh.HasValue && number > test.ReferenceHigh);
        }

        // One unverified result at a time: entering again corrects it
        var result = order.Results.FirstOrDefault(r => r.ReleasedAtUtc == null);
        if (result is null)
        {
            result = new LabResult { LabOrderId = order.Id, PatientId = order.PatientId };
            _db.Set<LabResult>().Add(result);
        }
        result.Value = value.Trim();
        result.Unit = test.Unit;
        result.ReferenceLow = test.ReferenceLow;
        result.ReferenceHigh = test.ReferenceHigh;
        result.IsAbnormal = abnormal;
        result.IsCritical = critical;
        result.EnteredByUserId = userId;

        order.Status = OrderStatus.InProgress;
        await _db.SaveChangesAsync(ct);
        return result;
    }

    /// <summary>
    /// A second technician verifies the result. It is then released to the record, the order is completed,
    /// the doctor is notified (critical values must be acknowledged) and billing is told (FR-LAB-007).
    /// </summary>
    public async Task VerifyAndReleaseAsync(Guid resultId, CancellationToken ct = default)
    {
        var userId = RequireUser();
        var result = await _db.Set<LabResult>().Include(r => r.LabOrder).ThenInclude(o => o.Test)
                         .FirstOrDefaultAsync(r => r.Id == resultId, ct)
                     ?? throw new BusinessRuleException("Result not found.");
        if (result.ReleasedAtUtc != null) throw new BusinessRuleException("This result was already released.");
        if (result.EnteredByUserId == userId)
            throw new BusinessRuleException("A different technician must verify this result. You entered it yourself.");

        var order = result.LabOrder;
        var now = DateTime.UtcNow;
        result.VerifiedByUserId = userId;
        result.VerifiedAtUtc = now;
        result.ReleasedAtUtc = now;
        order.Status = OrderStatus.Completed;

        // Close the sample if the technician did not
        var sample = await _db.Set<Sample>().FirstOrDefaultAsync(s => s.LabOrderId == order.Id && s.Status != SampleStatus.Completed, ct);
        if (sample is not null)
        {
            sample.Status = SampleStatus.Completed;
            sample.CompletedAtUtc = now;
            _db.Set<SampleStatusHistory>().Add(new SampleStatusHistory { SampleId = sample.Id, Status = SampleStatus.Completed, ByUserId = userId });
        }

        // Tell the ordering doctor, in the app
        var doctorUserId = await _db.Set<Staff>().Where(s => s.Id == order.OrderedByStaffId).Select(s => s.UserId).FirstOrDefaultAsync(ct);
        if (doctorUserId is not null)
            _db.Set<Notification>().Add(new Notification
            {
                UserId = doctorUserId.Value,
                Title = result.IsCritical ? "CRITICAL lab result" : "Lab result ready",
                Message = $"{order.Test.Name}: result released. Open Results to read it.",
                LinkUrl = "/Lab/Results",
                RequiresAcknowledgment = result.IsCritical
            });

        // FR-LAB-007: completed tests reach billing, saved in the same transaction
        var unit = await ClinicalLookup.CurrentPriceAsync(_db, order.Test.PriceItemId, ct);
        _events.Publish(new ServiceCompleted(order.PatientId, order.EncounterId, order.Test.PriceItemId, order.OrderedByStaffId,
            "Lab", order.Test.Name, 1, unit, now));

        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------ imaging files (FR-LAB-004)

    public async Task<ImagingStudy> AddImagingStudyAsync(Guid orderId, Stream? file, string? fileName, long fileLength,
        string? dicomLink, string? reportText, CancellationToken ct = default)
    {
        var userId = RequireUser();
        var order = await _db.Set<LabOrder>().Include(o => o.Test).FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw new BusinessRuleException("Order not found.");
        if (order.Test.Kind != TestKind.Radiology) throw new BusinessRuleException("Images can only be attached to radiology orders.");
        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled) throw new BusinessRuleException($"This order is {order.Status}.");

        var hasFile = file is not null && !string.IsNullOrWhiteSpace(fileName);
        if (!hasFile && string.IsNullOrWhiteSpace(dicomLink) && string.IsNullOrWhiteSpace(reportText))
            throw new BusinessRuleException("Upload a file, add a DICOM link, or write a report.");
        if (!string.IsNullOrWhiteSpace(dicomLink) &&
            !(Uri.TryCreate(dicomLink.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            throw new BusinessRuleException("The DICOM link must be a web address starting with http:// or https://.");

        var study = new ImagingStudy
        {
            LabOrderId = order.Id, PatientId = order.PatientId,
            DicomLink = Clean(dicomLink), ReportText = Clean(reportText)
        };

        if (hasFile)
        {
            var name = Path.GetFileName(fileName!);
            study.FilePath = await _files.SaveAsync(file!, name, fileLength, ct);

            // The file is also attached to the patient's record (EHR)
            _db.Set<PatientDocument>().Add(new PatientDocument
            {
                PatientId = order.PatientId, EncounterId = order.EncounterId, FileName = name,
                StoragePath = study.FilePath, ContentType = ContentTypeFor(name), SizeBytes = fileLength, UploadedByUserId = userId
            });
        }

        _db.Set<ImagingStudy>().Add(study);
        order.Status = OrderStatus.InProgress;
        await _db.SaveChangesAsync(ct);
        return study;
    }

    public Task<List<ImagingStudy>> GetImagingStudiesAsync(Guid orderId, CancellationToken ct = default) =>
        _db.Set<ImagingStudy>().AsNoTracking().Where(s => s.LabOrderId == orderId).OrderBy(s => s.CreatedAtUtc).ToListAsync(ct);

    public Task<ImagingStudy?> GetImagingStudyAsync(Guid studyId, CancellationToken ct = default) =>
        _db.Set<ImagingStudy>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == studyId, ct);

    public Task<Stream> OpenFileAsync(string storagePath, CancellationToken ct = default) => _files.OpenReadAsync(storagePath, ct);

    public static string ContentTypeFor(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        _ => "application/octet-stream"
    };

    // ------------------------------------------------------------ doctor side (FR-LAB-006)

    /// <summary>Released results of the tests this doctor ordered. Unverified results are never shown.</summary>
    public async Task<List<LabResult>> GetReleasedResultsAsync(CancellationToken ct = default)
    {
        var doctorId = _user.StaffId ?? throw new BusinessRuleException("Your account is not linked to a staff profile.");
        return await _db.Set<LabResult>().AsNoTracking()
            .Include(r => r.LabOrder).ThenInclude(o => o.Patient)
            .Include(r => r.LabOrder).ThenInclude(o => o.Test)
            .Where(r => r.ReleasedAtUtc != null && r.LabOrder.OrderedByStaffId == doctorId)
            .OrderByDescending(r => r.ReleasedAtUtc).Take(100).ToListAsync(ct);
    }

    public Task<List<Notification>> GetNotificationsAsync(CancellationToken ct = default)
    {
        var userId = RequireUser();
        return _db.Set<Notification>().AsNoTracking().Where(n => n.UserId == userId)
                  .OrderByDescending(n => n.CreatedAtUtc).Take(50).ToListAsync(ct);
    }

    /// <summary>The doctor confirms they have seen a critical value.</summary>
    public async Task AcknowledgeAsync(Guid notificationId, CancellationToken ct = default)
    {
        var userId = RequireUser();
        var n = await _db.Set<Notification>().FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, ct)
                ?? throw new BusinessRuleException("Notification not found.");
        n.IsRead = true;
        if (n.AcknowledgedAtUtc is null) n.AcknowledgedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Seat C asks this when the doctor finalizes a discharge: can the Laboratory clear the patient?</summary>
public class LabClearanceCheck : IDepartmentClearanceCheck
{
    private readonly IAppDbContext _db;
    public LabClearanceCheck(IAppDbContext db) => _db = db;
    public string DepartmentName => "Laboratory";

    public async Task<ClearanceCheckResult> CheckAsync(Guid encounterId, CancellationToken ct = default)
    {
        var open = await _db.Set<LabOrder>().CountAsync(o =>
            o.EncounterId == encounterId && (o.Status == OrderStatus.Ordered || o.Status == OrderStatus.InProgress), ct);
        return open == 0
            ? new ClearanceCheckResult(true, null)
            : new ClearanceCheckResult(false, $"{open} lab/radiology order(s) still in progress.");
    }
}
