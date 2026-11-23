using Microsoft.AspNetCore.Authorization;
using SmartCare.Application.Security;
using SmartCare.Application.WaitingRoom;
using SmartCare.Domain.Common;

namespace SmartCare.Web.Pages.WaitingRoom;

[Authorize(Roles = Roles.Receptionist + "," + Roles.Nurse + "," + Roles.Doctor + "," +
                   Roles.HospitalAdmin + "," + Roles.SuperAdmin)]
public class IndexModel : AppPageModel
{
    private readonly WaitingRoomService _svc;

    public IndexModel(WaitingRoomService svc) => _svc = svc;

    public Dictionary<JourneyStation, List<PatientAtStation>> Queues { get; set; } = new();

    public async Task OnGetAsync()
    {
        Queues = await _svc.GetAllQueuesAsync();
    }
}
