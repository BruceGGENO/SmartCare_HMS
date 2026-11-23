using Microsoft.EntityFrameworkCore;
using SmartCare.Application.Common;
using SmartCare.Domain.Common;
using SmartCare.Domain.Entities;

namespace SmartCare.Application.WaitingRoom;

public record PatientAtStation(
    Guid PatientId,
    string PatientName,
    string Mrn,
    JourneyStation Station,
    DateTime ArrivedAtUtc,
    double WaitMinutes);

public record StationWaitStats(
    JourneyStation Station,
    double AvgMinutes,
    double MedianMinutes,
    double P90Minutes,
    int EventCount);

public record HeatmapCell(
    JourneyStation Station,
    int HourOfDay,
    int DayOfWeek,
    double AvgWaitMinutes);

public class WaitingRoomService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _user;

    public WaitingRoomService(IAppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    // ── FR-WRA-001: record a timestamped journey event ────────────────────────
    public async Task<PatientJourneyEvent> RecordEventAsync(
        Guid patientId,
        Guid? appointmentId,
        Guid? encounterId,
        JourneyStation station,
        JourneyEventType eventType,
        CancellationToken ct = default)
    {
        if (!await _db.Set<Patient>().AnyAsync(p => p.Id == patientId, ct))
            throw new BusinessRuleException("Patient not found.");

        var evt = new PatientJourneyEvent
        {
            PatientId = patientId,
            AppointmentId = appointmentId,
            EncounterId = encounterId,
            Station = station,
            EventType = eventType,
            OccurredAtUtc = DateTime.UtcNow,
            RecordedByUserId = _user.UserId,
            IsManualCorrection = false
        };
        _db.Set<PatientJourneyEvent>().Add(evt);
        await _db.SaveChangesAsync(ct);
        return evt;
    }

    // ── FR-WRA-003: live queue — patients currently at a station ──────────────
    public async Task<List<PatientAtStation>> GetCurrentQueueAsync(
        JourneyStation station,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var lookback = now.AddHours(-12);

        var enters = await _db.Set<PatientJourneyEvent>().AsNoTracking()
            .Include(e => e.Patient)
            .Where(e => e.Station == station
                     && e.EventType == JourneyEventType.Enter
                     && e.OccurredAtUtc >= lookback)
            .ToListAsync(ct);

        var patientIds = enters.Select(e => e.PatientId).Distinct().ToList();

        var leaves = await _db.Set<PatientJourneyEvent>().AsNoTracking()
            .Where(e => e.Station == station
                     && e.EventType == JourneyEventType.Leave
                     && patientIds.Contains(e.PatientId)
                     && e.OccurredAtUtc >= lookback)
            .ToListAsync(ct);

        var result = new List<PatientAtStation>();
        foreach (var enter in enters.GroupBy(e => e.PatientId).Select(g => g.OrderByDescending(x => x.OccurredAtUtc).First()))
        {
            var hasLeft = leaves.Any(l => l.PatientId == enter.PatientId && l.OccurredAtUtc > enter.OccurredAtUtc);
            if (!hasLeft)
            {
                result.Add(new PatientAtStation(
                    enter.PatientId,
                    $"{enter.Patient.FirstName} {enter.Patient.LastName}",
                    enter.Patient.Mrn,
                    station,
                    enter.OccurredAtUtc,
                    (now - enter.OccurredAtUtc).TotalMinutes));
            }
        }
        return result.OrderBy(p => p.ArrivedAtUtc).ToList();
    }

    // All stations queue (for the dashboard)
    public async Task<Dictionary<JourneyStation, List<PatientAtStation>>> GetAllQueuesAsync(
        CancellationToken ct = default)
    {
        var dict = new Dictionary<JourneyStation, List<PatientAtStation>>();
        foreach (var station in Enum.GetValues<JourneyStation>())
            dict[station] = await GetCurrentQueueAsync(station, ct);
        return dict;
    }

    // ── FR-WRA-002, 005: compute avg / median / p90 wait times ───────────────
    public async Task<List<StationWaitStats>> GetWaitAnalyticsAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var events = await _db.Set<PatientJourneyEvent>().AsNoTracking()
            .Where(e => e.OccurredAtUtc >= from && e.OccurredAtUtc <= to)
            .OrderBy(e => e.PatientId).ThenBy(e => e.Station).ThenBy(e => e.OccurredAtUtc)
            .ToListAsync(ct);

        var stats = new List<StationWaitStats>();
        foreach (var station in Enum.GetValues<JourneyStation>())
        {
            var waits = ComputeWaits(events, station);
            if (waits.Count == 0) continue;
            var sorted = waits.OrderBy(w => w).ToList();
            stats.Add(new StationWaitStats(
                station,
                sorted.Average(),
                Percentile(sorted, 0.5),
                Percentile(sorted, 0.9),
                waits.Count));
        }
        return stats;
    }

    // ── FR-WRA-004: heatmap data (station × hour × weekday) ──────────────────
    public async Task<List<HeatmapCell>> GetHeatmapAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var events = await _db.Set<PatientJourneyEvent>().AsNoTracking()
            .Where(e => e.OccurredAtUtc >= from && e.OccurredAtUtc <= to)
            .OrderBy(e => e.PatientId).ThenBy(e => e.Station).ThenBy(e => e.OccurredAtUtc)
            .ToListAsync(ct);

        var cells = new List<HeatmapCell>();
        foreach (var station in Enum.GetValues<JourneyStation>())
        {
            var pairs = GetPairs(events, station);
            var grouped = pairs.GroupBy(p => (Hour: p.EnterTime.Hour, Day: (int)p.EnterTime.DayOfWeek));
            foreach (var g in grouped)
            {
                var avg = g.Average(p => (p.LeaveTime - p.EnterTime).TotalMinutes);
                cells.Add(new HeatmapCell(station, g.Key.Hour, g.Key.Day, avg));
            }
        }
        return cells;
    }

    // ── FR-WRA-006: bottleneck stations above target ──────────────────────────
    public async Task<List<StationWaitStats>> GetBottlenecksAsync(
        DateTime from, DateTime to, double targetMinutes, CancellationToken ct = default)
    {
        var all = await GetWaitAnalyticsAsync(from, to, ct);
        return all.Where(s => s.AvgMinutes > targetMinutes).ToList();
    }

    // ── FR-WRA-007: manual correction (new entry, never edits original) ───────
    public async Task<PatientJourneyEvent> CorrectEventAsync(
        Guid originalEventId,
        JourneyStation correctedStation,
        JourneyEventType correctedEventType,
        DateTime correctedTimeUtc,
        string reason,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleException("A reason is required for a manual correction.");

        var original = await _db.Set<PatientJourneyEvent>()
            .FirstOrDefaultAsync(e => e.Id == originalEventId, ct)
            ?? throw new BusinessRuleException("Original event not found.");

        var correction = new PatientJourneyEvent
        {
            PatientId = original.PatientId,
            AppointmentId = original.AppointmentId,
            EncounterId = original.EncounterId,
            Station = correctedStation,
            EventType = correctedEventType,
            OccurredAtUtc = correctedTimeUtc,
            RecordedByUserId = _user.UserId,
            IsManualCorrection = true,
            CorrectionReason = reason.Trim()
        };
        _db.Set<PatientJourneyEvent>().Add(correction);
        await _db.SaveChangesAsync(ct);
        return correction;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private record Pair(DateTime EnterTime, DateTime LeaveTime);

    private static List<Pair> GetPairs(List<PatientJourneyEvent> events, JourneyStation station)
    {
        var pairs = new List<Pair>();
        foreach (var byPatient in events.Where(e => e.Station == station).GroupBy(e => e.PatientId))
        {
            DateTime? enterTime = null;
            foreach (var e in byPatient.OrderBy(x => x.OccurredAtUtc))
            {
                if (e.EventType == JourneyEventType.Enter)
                {
                    enterTime = e.OccurredAtUtc;
                }
                else if (e.EventType == JourneyEventType.Leave && enterTime.HasValue)
                {
                    pairs.Add(new Pair(enterTime.Value, e.OccurredAtUtc));
                    enterTime = null;
                }
            }
        }
        return pairs;
    }

    private static List<double> ComputeWaits(List<PatientJourneyEvent> events, JourneyStation station) =>
        GetPairs(events, station).Select(p => (p.LeaveTime - p.EnterTime).TotalMinutes).ToList();

    private static double Percentile(List<double> sorted, double p)
    {
        if (sorted.Count == 0) return 0;
        var idx = p * (sorted.Count - 1);
        var lower = (int)Math.Floor(idx);
        var upper = (int)Math.Ceiling(idx);
        if (lower == upper) return sorted[lower];
        return sorted[lower] + (sorted[upper] - sorted[lower]) * (idx - lower);
    }
}
