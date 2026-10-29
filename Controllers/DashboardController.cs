using FootballClubManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace FootballClubManagement.Controllers;

public sealed class DashboardController : AppController
{
    private const string TourKey = "DashboardTourSeen";

    private readonly DashboardService _dashboard;

    public DashboardController(DashboardService dashboard) => _dashboard = dashboard;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var seen = HttpContext.Session.GetString(TourKey) == "1";

        var vm = await _dashboard.BuildAsync(
            CurrentRole, CurrentPersonId, CurrentClubId, CurrentDisplayName, showGettingStarted: !seen);

        HttpContext.Session.SetString(TourKey, "1");

        return View(vm);
    }
}
