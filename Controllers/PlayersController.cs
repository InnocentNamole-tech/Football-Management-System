using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FootballClubManagement.Controllers;

[Authorize(Roles = RoleNames.Coordinator + "," + RoleNames.Coach)]
public sealed class PlayersController : AppController
{
    private readonly PlayerService _players;
    private readonly LookupService _lookups;

    public PlayersController(PlayerService players, LookupService lookups)
    {
        _players = players;
        _lookups = lookups;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        ViewBag.CanEdit = CurrentRole == RoleNames.Coordinator;
        ViewBag.ClubName = (await _lookups.ClubAsync(clubId))?.ClubName;
        return View(await _players.ListAsync(clubId));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var player = await _players.GetAsync(id, clubId);
        if (player is null) return NotFound();

        ViewBag.CanEdit = CurrentRole == RoleNames.Coordinator;
        return View(player);
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateLookupsAsync();
        return View(new PlayerCreateVm());
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> Create(PlayerCreateVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync();
            return View(vm);
        }

        var result = await _players.CreateAsync(vm, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateLookupsAsync();
            return View(vm);
        }

        FlashSuccess($"Player {vm.FirstName} {vm.LastName} was registered.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var vm = await _players.GetEditVmAsync(id, clubId);
        if (vm is null) return NotFound();

        await PopulateLookupsAsync();
        return View(vm);
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> Edit(PlayerEditVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync();
            return View(vm);
        }

        var result = await _players.UpdateAsync(vm, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateLookupsAsync();
            return View(vm);
        }

        FlashSuccess($"Player {vm.FirstName} {vm.LastName} was updated.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var player = await _players.GetAsync(id, clubId);
        if (player is null) return NotFound();

        return View(player);
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> DeactivateConfirm(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var result = await _players.DeactivateAsync(id, clubId);
        if (result.Ok)
        {
            FlashSuccess("The player has been deactivated and their jersey number released.");
        }
        else
        {
            FlashError(result.Message ?? "That player could not be deactivated.");
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync()
    {
        ViewBag.Cities = new SelectList(await _lookups.CitiesAsync(), "Id", "Name");
        ViewBag.Positions = new SelectList(await _lookups.PositionsAsync(), "Id", "Name");
        ViewBag.Styles = new SelectList(await _lookups.PlayerStylesAsync(), "Id", "Name");
    }
}
