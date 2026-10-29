using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballClubManagement.Controllers;

[Authorize(Roles = RoleNames.Coordinator + "," + RoleNames.Coach + "," + RoleNames.Player + "," + RoleNames.Fan)]
public sealed class TeamHistoryController : AppController
{
    private readonly TeamHistoryService _histories;
    private readonly LookupService _lookups;

    public TeamHistoryController(TeamHistoryService histories, LookupService lookups)
    {
        _histories = histories;
        _lookups = lookups;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        ViewBag.CanManage = CurrentRole == RoleNames.Coordinator;
        ViewBag.ClubName = (await _lookups.ClubAsync(clubId))?.ClubName;
        return View(await _histories.ListAsync(clubId));
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        ViewBag.ClubName = (await _lookups.ClubAsync(clubId))?.ClubName;
        return View(new TeamHistoryVm());
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> Create(TeamHistoryVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid)
        {
            ViewBag.ClubName = (await _lookups.ClubAsync(clubId))?.ClubName;
            return View(vm);
        }

        var result = await _histories.CreateAsync(vm, CurrentPersonId, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            ViewBag.ClubName = (await _lookups.ClubAsync(clubId))?.ClubName;
            return View(vm);
        }

        FlashSuccess("The club history entry has been added.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var vm = await _histories.GetEditVmAsync(id, clubId);
        if (vm is null) return NotFound();

        ViewBag.ClubName = (await _lookups.ClubAsync(clubId))?.ClubName;
        return View(vm);
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> Edit(TeamHistoryVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid)
        {
            ViewBag.ClubName = (await _lookups.ClubAsync(clubId))?.ClubName;
            return View(vm);
        }

        var result = await _histories.UpdateAsync(vm, CurrentPersonId, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            ViewBag.ClubName = (await _lookups.ClubAsync(clubId))?.ClubName;
            return View(vm);
        }

        FlashSuccess("The club history entry has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var row = await _histories.GetAsync(id, clubId);
        if (row is null) return NotFound();

        ViewBag.ClubName = (await _lookups.ClubAsync(clubId))?.ClubName;
        return View(row);
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var result = await _histories.DeleteAsync(id, CurrentPersonId, clubId);
        if (!result.Ok)
        {
            FlashError(result.Message ?? "The club history entry could not be deleted.");
            return RedirectToAction(nameof(Index));
        }

        FlashSuccess("The club history entry has been deleted.");
        return RedirectToAction(nameof(Index));
    }
}
