using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballClubManagement.Controllers;

public sealed class ClubsController : AppController
{
    private readonly ClubService _clubs;
    private readonly LocationService _locations;

    public ClubsController(ClubService clubs, LocationService locations)
    {
        _clubs = clubs;
        _locations = locations;
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["PrimaryText"] = "Register club";
        ViewData["PrimaryUrl"] = Url.Action(nameof(Create));
        return View(await _clubs.ListAsync());
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var clubScoped = CurrentRole is RoleNames.Coordinator or RoleNames.Coach or RoleNames.Player or RoleNames.Fan;
        if (!clubScoped && CurrentRole != RoleNames.Admin) return AccessDeniedPage();
        if (clubScoped && (!TryCurrentClub(out var clubId) || id != clubId))
        {
            return AccessDeniedPage();
        }

        var club = await _clubs.GetAsync(id);
        if (club is null) return NotFound();

        ViewBag.CanBrowseAll = CurrentRole == RoleNames.Admin;
        return View(club);
    }

    [Authorize(Roles = RoleNames.Coordinator + "," + RoleNames.Coach + "," + RoleNames.Player + "," + RoleNames.Fan)]
    [HttpGet]
    public async Task<IActionResult> MyClub()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var overview = await _clubs.OverviewAsync(clubId);
        if (overview is null) return NotFound();

        return View(overview);
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateLocationsAsync();
        return View(new ClubCreateVm());
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(ClubCreateVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLocationsAsync();
            return View(vm);
        }

        var result = await _clubs.CreateAsync(vm);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateLocationsAsync();
            return View(vm);
        }

        FlashSuccess($"{vm.ClubName} was registered.");
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    private async Task PopulateLocationsAsync()
    {
        ViewBag.Cities = await _locations.CitiesAsync();
        ViewBag.Stadiums = await _locations.StadiumsAsync();
    }
}
