using System.Collections.Concurrent;
using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;

namespace SmartCare.Infrastructure.Services;

public sealed class WaitingRoomService : IWaitingRoomService
{
    private readonly ISqlExecutor _sql;
    private static readonly List<PatientJourneyEventDto> _events = new();
    private static readonly object _lock = new();

    static WaitingRoomService()
    {
        var now = DateTime.UtcNow;
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();

        _events.AddRange([
            new PatientJourneyEventDto { Id = Guid.NewGuid(), PatientId = p1, PatientName = "Jean Paul Ndayisaba", PatientMrn = "MRN-2026-0045", Station = "Registration", EventType = "Enter", OccurredAtUtc = now.AddMinutes(-55), WaitMinutes = 55 },
            new PatientJourneyEventDto { Id = Guid.NewGuid(), PatientId = p1, PatientName = "Jean Paul Ndayisaba", PatientMrn = "MRN-2026-0045", Station = "Registration", EventType = "Leave", OccurredAtUtc = now.AddMinutes(-40), WaitMinutes = 15 },
            new PatientJourneyEventDto { Id = Guid.NewGuid(), PatientId = p1, PatientName = "Jean Paul Ndayisaba", PatientMrn = "MRN-2026-0045", Station = "Triage", EventType = "Enter", OccurredAtUtc = now.AddMinutes(-38), WaitMinutes = 38 },
            new PatientJourneyEventDto { Id = Guid.NewGuid(), PatientId = p2, PatientName = "Aline Mukamana", PatientMrn = "MRN-2026-0089", Station = "Doctor", EventType = "Enter", OccurredAtUtc = now.AddMinutes(-25), WaitMinutes = 25 },
            new PatientJourneyEventDto { Id = Guid.NewGuid(), PatientId = p3, PatientName = "David Habimana", PatientMrn = "MRN-2026-0102", Station = "Laboratory", EventType = "Enter", OccurredAtUtc = now.AddMinutes(-15), WaitMinutes = 15 }
        ]);
    }

    public WaitingRoomService(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public Task<IReadOnlyList<PatientJourneyEventDto>> GetLiveQueueAsync(string? station = null, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            // A patient is waiting if their latest event is "Enter"
            var active = _events
                .GroupBy(e => e.PatientId)
                .Select(g => g.OrderByDescending(x => x.OccurredAtUtc).First())
                .Where(e => e.EventType == "Enter")
                .Where(e => string.IsNullOrWhiteSpace(station) || e.Station.Equals(station, StringComparison.OrdinalIgnoreCase))
                .Select(e => new PatientJourneyEventDto
                {
                    Id = e.Id,
                    PatientId = e.PatientId,
                    PatientName = e.PatientName,
                    PatientMrn = e.PatientMrn,
                    Station = e.Station,
                    EventType = e.EventType,
                    OccurredAtUtc = e.OccurredAtUtc,
                    WaitMinutes = Math.Round((now - e.OccurredAtUtc).TotalMinutes, 1)
                })
                .OrderByDescending(e => e.WaitMinutes)
                .ToList();

            return Task.FromResult<IReadOnlyList<PatientJourneyEventDto>>(active);
        }
    }

    public Task<PatientJourneyEventDto> RecordEventAsync(RecordJourneyEventRequest request, Guid? userId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var evt = new PatientJourneyEventDto
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId != Guid.Empty ? request.PatientId : Guid.NewGuid(),
                PatientName = string.IsNullOrWhiteSpace(request.PatientName) ? "Patient" : request.PatientName.Trim(),
                PatientMrn = string.IsNullOrWhiteSpace(request.PatientMrn) ? "MRN-DEMO" : request.PatientMrn.Trim(),
                Station = request.Station.Trim(),
                EventType = request.EventType.Trim(),
                OccurredAtUtc = now,
                WaitMinutes = 0
            };
            _events.Add(evt);
            return Task.FromResult(evt);
        }
    }

    public Task<IReadOnlyList<StationWaitAnalyticsDto>> GetAnalyticsAsync(DateTime? from, DateTime? to, double targetMinutes = 30, CancellationToken cancellationToken = default)
    {
        var stations = new[] { "Registration", "Triage", "Doctor", "Laboratory", "Pharmacy", "Billing", "Radiology" };
        var list = new List<StationWaitAnalyticsDto>();

        var random = new Random(42);
        foreach (var s in stations)
        {
            var avg = s switch
            {
                "Doctor" => 38.5,
                "Laboratory" => 32.0,
                "Pharmacy" => 22.4,
                "Triage" => 14.2,
                "Registration" => 12.0,
                _ => 18.0
            };
            list.Add(new StationWaitAnalyticsDto
            {
                Station = s,
                AvgMinutes = avg,
                MedianMinutes = avg * 0.9,
                P90Minutes = avg * 1.6,
                EventCount = random.Next(15, 60),
                IsBottleneck = avg > targetMinutes
            });
        }

        return Task.FromResult<IReadOnlyList<StationWaitAnalyticsDto>>(list);
    }

    public Task<IReadOnlyList<HeatmapCellDto>> GetHeatmapAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var stations = new[] { "Registration", "Triage", "Doctor", "Laboratory", "Pharmacy", "Billing" };
        var list = new List<HeatmapCellDto>();

        for (int hour = 8; hour <= 18; hour++)
        {
            foreach (var s in stations)
            {
                var peakMultiplier = (hour >= 10 && hour <= 14) ? 1.8 : 1.0;
                var baseWait = s == "Doctor" ? 25.0 : s == "Laboratory" ? 20.0 : 10.0;
                list.Add(new HeatmapCellDto
                {
                    Station = s,
                    HourOfDay = hour,
                    AvgWaitMinutes = Math.Round(baseWait * peakMultiplier, 1)
                });
            }
        }

        return Task.FromResult<IReadOnlyList<HeatmapCellDto>>(list);
    }

    public Task<bool> CorrectEventAsync(CorrectJourneyEventRequest request, Guid? userId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var orig = _events.FirstOrDefault(e => e.Id == request.EventId);
            if (orig is null) return Task.FromResult(false);

            var correction = new PatientJourneyEventDto
            {
                Id = Guid.NewGuid(),
                PatientId = orig.PatientId,
                PatientName = orig.PatientName,
                PatientMrn = orig.PatientMrn,
                Station = request.CorrectedStation,
                EventType = request.CorrectedEventType,
                OccurredAtUtc = request.CorrectedTimeUtc,
                IsManualCorrection = true,
                CorrectionReason = request.Reason
            };
            _events.Add(correction);
            return Task.FromResult(true);
        }
    }
}
