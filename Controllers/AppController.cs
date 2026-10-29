using System.Security.Claims;
using FootballClubManagement.Models;
using FootballClubManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballClubManagement.Controllers;

[Authorize]
public abstract class AppController : Controller
{
    protected int CurrentPersonId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    protected string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? "";

    protected string CurrentDisplayName =>
        User.FindFirstValue(AppClaims.FullName) ?? User.Identity?.Name ?? "User";

    protected int? CurrentClubId =>
        int.TryParse(User.FindFirstValue(AppClaims.ClubId), out var id) ? id : null;

    protected bool TryCurrentClub(out int clubId)
    {
        clubId = CurrentClubId ?? 0;
        return clubId > 0;
    }

    protected IActionResult AccessDeniedPage() => RedirectToAction("AccessDenied", "Error");

    protected void FlashSuccess(string message) => TempData["Success"] = message;

    protected void FlashError(string message) => TempData["Error"] = message;

    protected void ApplyErrors(ServiceResult result)
    {
        if (result.Message is null) return;

        if (result.Field is not null)
        {
            ModelState.AddModelError(result.Field, result.Message);
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.Message);
        }
    }
}
