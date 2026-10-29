using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FootballClubManagement.Controllers;

[Authorize(Roles = RoleNames.Coordinator)]
public sealed class CoachesController : AppController
{
    private readonly CoachService _coaches;
    private readonly LookupService _lookups;

    public CoachesController(CoachService coaches, LookupService lookups)
    {
        _coaches = coaches;
        _lookups = lookups;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        ViewBag.ClubName = (await _lookups.ClubAsync(clubId))?.ClubName;
        return View(await _coaches.ListAsync(clubId));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateLookupsAsync();
        return View(new CoachCreateVm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CoachCreateVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync();
            return View(vm);
        }

        var result = await _coaches.CreateAsync(vm, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateLookupsAsync();
            return View(vm);
        }

        FlashSuccess($"Coach {vm.FirstName} {vm.LastName} was registered.");
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync()
    {
        ViewBag.Specializations = new SelectList(await _lookups.SpecializationsAsync(), "Id", "Name");
    }
}
