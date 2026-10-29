using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FootballClubManagement.Controllers;

[Authorize(Roles = RoleNames.Coordinator + "," + RoleNames.Coach + "," + RoleNames.Player + "," + RoleNames.Fan)]
public sealed class CommentsController : AppController
{
    private readonly CommentService _comments;

    public CommentsController(CommentService comments) => _comments = comments;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        ViewBag.PersonId = CurrentPersonId;
        return View(await _comments.ListAsync(clubId));
    }

    [Authorize(Roles = RoleNames.Fan)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        await PopulateMatchesAsync(clubId);
        return View(new CommentVm());
    }

    [Authorize(Roles = RoleNames.Fan)]
    [HttpPost]
    public async Task<IActionResult> Create(CommentVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid)
        {
            await PopulateMatchesAsync(clubId);
            return View(vm);
        }

        var result = await _comments.CreateAsync(vm, CurrentPersonId, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateMatchesAsync(clubId);
            return View(vm);
        }

        FlashSuccess("Your comment has been posted.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Fan)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var vm = await _comments.GetEditVmAsync(id, CurrentPersonId, clubId);
        if (vm is null) return NotFound();

        await PopulateMatchesAsync(clubId);
        return View(vm);
    }

    [Authorize(Roles = RoleNames.Fan)]
    [HttpPost]
    public async Task<IActionResult> Edit(CommentVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid)
        {
            await PopulateMatchesAsync(clubId);
            return View(vm);
        }

        var result = await _comments.UpdateAsync(vm, CurrentPersonId, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateMatchesAsync(clubId);
            return View(vm);
        }

        FlashSuccess("Your comment has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Fan)]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var row = await _comments.GetOwnAsync(id, CurrentPersonId, clubId);
        if (row is null) return NotFound();
        return View(row);
    }

    [Authorize(Roles = RoleNames.Fan)]
    [HttpPost]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var result = await _comments.DeleteAsync(id, CurrentPersonId, clubId);
        if (!result.Ok)
        {
            FlashError(result.Message ?? "That comment could not be deleted.");
            return RedirectToAction(nameof(Index));
        }

        FlashSuccess("Your comment has been deleted.");
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateMatchesAsync(int clubId)
    {
        ViewBag.Matches = new SelectList(await _comments.GetMatchOptionsAsync(clubId), "Id", "Name");
    }
}
