using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballClubManagement.Controllers;

[Authorize(Roles = RoleNames.Coordinator + "," + RoleNames.Coach + "," + RoleNames.Player + "," + RoleNames.Fan)]
public sealed class AnnouncementsController : AppController
{
    private readonly AnnouncementService _announcements;

    public AnnouncementsController(AnnouncementService announcements) => _announcements = announcements;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        ViewBag.CanPost = CurrentRole == RoleNames.Coordinator;
        ViewBag.OwnCoordinatorId = CurrentRole == RoleNames.Coordinator
            ? await _announcements.GetCoordinatorIdAsync(CurrentPersonId)
            : null;
        return View(await _announcements.ListAsync(clubId));
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public IActionResult Create() => View(new AnnouncementVm());

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> Create(AnnouncementVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid) return View(vm);

        var result = await _announcements.CreateAsync(vm, CurrentPersonId, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            return View(vm);
        }

        FlashSuccess("The announcement has been posted.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var vm = await _announcements.GetEditVmAsync(id, clubId);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> Edit(AnnouncementVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid) return View(vm);

        var result = await _announcements.UpdateAsync(vm, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            return View(vm);
        }

        FlashSuccess("The announcement has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var row = await _announcements.GetOwnAsync(id, CurrentPersonId, clubId);
        if (row is null) return NotFound();
        return View(row);
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var result = await _announcements.DeleteAsync(id, CurrentPersonId, clubId);
        if (!result.Ok)
        {
            FlashError(result.Message ?? "That announcement could not be deleted.");
            return RedirectToAction(nameof(Index));
        }

        FlashSuccess("The announcement has been deleted.");
        return RedirectToAction(nameof(Index));
    }
}
