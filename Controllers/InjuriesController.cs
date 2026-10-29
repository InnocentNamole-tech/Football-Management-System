using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FootballClubManagement.Controllers;

[Authorize(Roles = RoleNames.Coordinator + "," + RoleNames.Coach + "," + RoleNames.Player)]
public sealed class InjuriesController : AppController
{
    private readonly InjuryService _injuries;
    private readonly PlayerService _players;

    public InjuriesController(InjuryService injuries, PlayerService players)
    {
        _injuries = injuries;
        _players = players;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (CurrentRole == RoleNames.Player)
        {
            var player = await _players.GetByPersonAsync(CurrentPersonId);
            if (player is null) return NotFound();

            ViewBag.OwnOnly = true;
            ViewBag.CanEdit = false;
            return View(await _injuries.ListForPlayerAsync(CurrentPersonId));
        }

        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        ViewBag.OwnOnly = false;
        ViewBag.CanEdit = CurrentRole == RoleNames.Coordinator;
        return View(await _injuries.ListForClubAsync(clubId));
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Record()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        await PopulatePlayersAsync(clubId);
        return View(new InjuryVm());
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> Record(InjuryVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid)
        {
            await PopulatePlayersAsync(clubId);
            return View(vm);
        }

        var result = await _injuries.CreateAsync(vm, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulatePlayersAsync(clubId);
            return View(vm);
        }

        FlashSuccess("The injury has been recorded.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var vm = await _injuries.GetEditVmAsync(id, clubId);
        if (vm is null) return NotFound();

        await PopulatePlayersAsync(clubId);
        return View(vm);
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> Edit(InjuryVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid)
        {
            await PopulatePlayersAsync(clubId);
            return View(vm);
        }

        var result = await _injuries.UpdateAsync(vm, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulatePlayersAsync(clubId);
            return View(vm);
        }

        FlashSuccess("The injury record has been updated.");
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulatePlayersAsync(int clubId)
    {
        ViewBag.Players = new SelectList(await _players.ListAsync(clubId), "PlayerId", "FullName");
    }
}
