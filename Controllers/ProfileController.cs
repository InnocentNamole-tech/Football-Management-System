using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FootballClubManagement.Controllers;

public sealed class ProfileController : AppController
{
    private readonly ProfileService _profiles;

    public ProfileController(ProfileService profiles) => _profiles = profiles;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = await _profiles.GetAsync(CurrentPersonId);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var profile = await _profiles.GetAsync(CurrentPersonId);
        if (profile is null) return NotFound();

        return View(new ProfileEditVm
        {
            FirstName = profile.Person.FirstName,
            LastName = profile.Person.LastName,
            Email = profile.Person.Email,
            Phone = profile.Person.Phone
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProfileEditVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _profiles.UpdateAsync(CurrentPersonId, vm);
        if (!result.Ok)
        {
            ApplyErrors(result);
            return View(vm);
        }

        FlashSuccess(string.IsNullOrEmpty(vm.NewPassword)
            ? "Your profile has been updated."
            : "Your profile and password have been updated.");
        return RedirectToAction(nameof(Index));
    }
}
