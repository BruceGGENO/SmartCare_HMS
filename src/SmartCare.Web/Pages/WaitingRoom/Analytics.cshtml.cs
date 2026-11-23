using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Security;
using SmartCare.Application.WaitingRoom;

namespace SmartCare.Web.Pages.WaitingRoom;

[Authorize(Roles = Roles.HospitalAdmin + "," + Roles.SuperAdmin)]
public class AnalyticsModel : AppPageModel
{
    private readonly WaitingRoomService _svc;

    public AnalyticsModel(WaitingRoomService svc) => _svc = svc;

    public List<StationWaitStats> Stats { get; set; } = new();
    public List<HeatmapCell> Heatmap { get; set; } = new();
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public double TargetMinutes { get; set; } = 30;

    public Task OnGetAsync()
    {
        // Default: last 7 days
        To = DateTime.UtcNow;
        From = To.AddDays(-7);
        return LoadDataAsync();
    }

    public Task<IActionResult> OnPostAsync(DateTime from, DateTime to, double targetMinutes)
    {
        From = from;
        To = to;
        TargetMinutes = targetMinutes > 0 ? targetMinutes : 30;
        return LoadDataAndReturnPageAsync();
    }

    private async Task LoadDataAsync()
    {
        Stats = await _svc.GetWaitAnalyticsAsync(From, To);
        Heatmap = await _svc.GetHeatmapAsync(From, To);
    }

    private async Task<IActionResult> LoadDataAndReturnPageAsync()
    {
        await LoadDataAsync();
        return Page();
    }
}
