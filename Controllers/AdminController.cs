using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballClubManagement.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminController : AppController
{
    private readonly AccountService _accounts;
    private readonly LookupService _lookups;

    public AdminController(AccountService accounts, LookupService lookups)
    {
        _accounts = accounts;
        _lookups = lookups;
    }

    [HttpGet]
    public async Task<IActionResult> Accounts(string? search)
    {
        ViewData["Search"] = search;
        ViewData["CurrentPersonId"] = CurrentPersonId;
        return View(await _accounts.GetAccountsAsync(search));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateLookupsAsync();
        return View(new CreateAccountVm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync();
            return View(vm);
        }

        var result = await _accounts.CreateAsync(vm);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateLookupsAsync();
            return View(vm);
        }

        FlashSuccess($"Account for {vm.FirstName} {vm.LastName} was created.");
        return RedirectToAction(nameof(Accounts));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var person = await _accounts.GetAsync(id);
        if (person is null) return NotFound();

        return View(new ResetPasswordVm { PersonId = person.PersonId, PersonName = person.FullName });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _accounts.ResetPasswordAsync(vm.PersonId, vm.NewPassword);
        if (!result.Ok)
        {
            ApplyErrors(result);
            return View(vm);
        }

        FlashSuccess($"Password reset for {vm.PersonName}.");
        return RedirectToAction(nameof(Accounts));
    }

    [HttpPost]
    public async Task<IActionResult> Unlock(int id)
    {
        var result = await _accounts.UnlockAsync(id);
        if (result.Ok)
        {
            FlashSuccess("The account has been unlocked.");
        }
        else
        {
            FlashError(result.Message ?? "That account could not be unlocked.");
        }

        return RedirectToAction(nameof(Accounts));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        if (id == CurrentPersonId) return AccessDeniedPage();

        var account = await _accounts.GetDeleteContextAsync(id);
        if (account is null) return NotFound();

        return View(account);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _accounts.DeleteAsync(id, CurrentPersonId);
        if (result.Ok)
        {
            FlashSuccess("The account was removed.");
            return RedirectToAction(nameof(Accounts));
        }

        FlashError(result.Message ?? "That account could not be removed.");
        return RedirectToAction(nameof(Delete), new { id });
    }

    private async Task PopulateLookupsAsync()
    {
        ViewBag.Clubs = await _lookups.ClubsAsync();
        ViewBag.Cities = await _lookups.CitiesAsync();
        ViewBag.Positions = await _lookups.PositionsAsync();
        ViewBag.Styles = await _lookups.PlayerStylesAsync();
        ViewBag.Specializations = await _lookups.SpecializationsAsync();
    }
}
