using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballClubManagement.Controllers;

[Authorize(Roles = RoleNames.Coordinator + "," + RoleNames.Coach + "," + RoleNames.Player)]
public sealed class UnavailabilitiesController : AppController
{
    private readonly UnavailabilityService _unavailabilities;

    public UnavailabilitiesController(UnavailabilityService unavailabilities)
    {
        _unavailabilities = unavailabilities;
    }

    [Authorize(Roles = RoleNames.Player)]
    [HttpGet]
    public async Task<IActionResult> Index() => View(await _unavailabilities.ListForPlayerAsync(CurrentPersonId));

    [Authorize(Roles = RoleNames.Coordinator + "," + RoleNames.Coach)]
    [HttpGet]
    public async Task<IActionResult> Club()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();
        return View(await _unavailabilities.ListForClubAsync(clubId));
    }

    [Authorize(Roles = RoleNames.Player)]
    [HttpGet]
    public IActionResult Create() => View(new UnavailabilityVm());

    [Authorize(Roles = RoleNames.Player)]
    [HttpPost]
    public async Task<IActionResult> Create(UnavailabilityVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _unavailabilities.CreateAsync(vm, CurrentPersonId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            return View(vm);
        }

        FlashSuccess("Your unavailability has been added.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Player)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _unavailabilities.GetEditVmAsync(id, CurrentPersonId);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = RoleNames.Player)]
    [HttpPost]
    public async Task<IActionResult> Edit(UnavailabilityVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _unavailabilities.UpdateAsync(vm, CurrentPersonId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            return View(vm);
        }

        FlashSuccess("Your unavailability has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Player)]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var row = await _unavailabilities.GetOwnAsync(id, CurrentPersonId);
        if (row is null) return NotFound();
        return View(row);
    }

    [Authorize(Roles = RoleNames.Player)]
    [HttpPost]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _unavailabilities.DeleteAsync(id, CurrentPersonId);
        if (!result.Ok)
        {
            FlashError(result.Message ?? "That unavailability record could not be deleted.");
            return RedirectToAction(nameof(Index));
        }

        FlashSuccess("Your unavailability has been deleted.");
        return RedirectToAction(nameof(Index));
    }
}
