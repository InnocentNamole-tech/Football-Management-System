using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FootballClubManagement.Controllers;

[Authorize]
public sealed class MatchesController : AppController
{
    private readonly MatchService _matches;
    private readonly CompetitionService _competitions;
    private readonly LookupService _lookups;

    public MatchesController(MatchService matches, CompetitionService competitions, LookupService lookups)
    {
        _matches = matches;
        _competitions = competitions;
        _lookups = lookups;
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        ViewBag.ClubName = (await _lookups.ClubAsync(clubId))?.ClubName;
        return View(await _matches.ListAsync(clubId));
    }

    [Authorize(Roles = RoleNames.Coordinator + "," + RoleNames.Player + "," + RoleNames.Fan + "," + RoleNames.Coach)]
    [HttpGet]
    public async Task<IActionResult> Fixtures()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        ViewBag.ClubName = (await _lookups.ClubAsync(clubId))?.ClubName;
        return View(await _matches.ListAsync(clubId, "fixtures"));
    }

    [Authorize(Roles = RoleNames.Coordinator + "," + RoleNames.Player + "," + RoleNames.Fan + "," + RoleNames.Coach)]
    [HttpGet]
    public async Task<IActionResult> Results()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        ViewBag.ClubName = (await _lookups.ClubAsync(clubId))?.ClubName;
        return View(await _matches.ListAsync(clubId, "results"));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var match = await _matches.GetAsync(id, clubId);
        if (match is null) return NotFound();

        ViewBag.CanEdit = CurrentRole == RoleNames.Coordinator;
        ViewBag.CanManageSquad = CurrentRole == RoleNames.Coach;
        return View(match);
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateLookupsAsync();
        return View(new MatchCreateVm());
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> Create(MatchCreateVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync();
            return View(vm);
        }

        var result = await _matches.CreateAsync(vm, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateLookupsAsync();
            return View(vm);
        }

        FlashSuccess("The match has been added to the fixture list.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> EditResult(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var vm = await _matches.GetResultVmAsync(id, clubId);
        if (vm is null) return NotFound();

        return View(vm);
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> EditResult(MatchResultVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid) return View(vm);

        var result = await _matches.SetResultAsync(vm, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            return View(vm);
        }

        FlashSuccess("The match result has been saved.");
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync()
    {
        ViewBag.Competitions = new SelectList(await _competitions.ApprovedOptionsAsync(CurrentClubId ?? 0), "Id", "Name");
        ViewBag.Stadiums = new SelectList(await _lookups.StadiumsAsync(), "Id", "Name");
    }
}
