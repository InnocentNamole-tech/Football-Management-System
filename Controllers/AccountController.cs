using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballClubManagement.Controllers;

[AllowAnonymous]
public sealed class AccountController : Controller
{
    private const string ResetPersonKey = "PasswordResetPersonId";

    private readonly AuthService _auth;
    private readonly AccountService _accounts;
    private readonly LookupService _lookups;

    public AccountController(AuthService auth, AccountService accounts, LookupService lookups)
    {
        _auth = auth;
        _accounts = accounts;
        _lookups = lookups;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new LoginVm());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _auth.LoginAsync(vm.Username, vm.Password);

        if (!result.Success || result.Principal is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Invalid username or password.");
            return View(vm);
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            result.Principal,
            new AuthenticationProperties { IsPersistent = false });

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
    }

    [HttpGet]
    public Task<IActionResult> RegisterFan() => RegisterViewAsync(RoleNames.Fan, new RegisterVm());

    [HttpPost]
    public Task<IActionResult> RegisterFan(RegisterVm vm) => RegisterPostAsync(RoleNames.Fan, vm);

    [HttpGet]
    public Task<IActionResult> RegisterOrganiser() => RegisterViewAsync(RoleNames.Organiser, new RegisterVm());

    [HttpPost]
    public Task<IActionResult> RegisterOrganiser(RegisterVm vm) => RegisterPostAsync(RoleNames.Organiser, vm);

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new ForgotPasswordVm());
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _auth.FindForPasswordResetAsync(vm.Email, vm.Username);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "No match found for this user name.");
            return View(vm);
        }

        HttpContext.Session.SetInt32(ResetPersonKey, user.PersonId);
        return RedirectToAction(nameof(ResetPassword));
    }

    [HttpGet]
    public IActionResult ResetPassword()
    {
        if (HttpContext.Session.GetInt32(ResetPersonKey) is null)
        {
            TempData["Error"] = "Confirm your email address and username before choosing a new password.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new SelfResetPasswordVm());
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(SelfResetPasswordVm vm)
    {
        var personId = HttpContext.Session.GetInt32(ResetPersonKey);
        if (personId is null)
        {
            TempData["Error"] = "Your reset session has expired. Confirm your email address and username again.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        if (!ModelState.IsValid) return View(vm);

        var result = await _accounts.ResetPasswordAsync(personId.Value, vm.NewPassword);
        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "The password could not be updated.");
            return View(vm);
        }

        HttpContext.Session.Remove(ResetPersonKey);
        TempData["Success"] = "Your password has been updated. Sign in with your new password.";
        return RedirectToAction(nameof(Login));
    }

    private async Task<IActionResult> RegisterViewAsync(string role, RegisterVm vm)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Role = role;
        ViewBag.Clubs = await _lookups.ClubsAsync();
        ViewData["Title"] = role == RoleNames.Fan ? "Create fan account" : "Create organiser account";
        return View("RegisterRole", vm);
    }

    private async Task<IActionResult> RegisterPostAsync(string role, RegisterVm vm)
    {
        if (role == RoleNames.Fan && vm.ClubId is null)
        {
            ModelState.AddModelError(nameof(vm.ClubId), "Choose the club you support.");
        }

        if (!ModelState.IsValid)
        {
            return await RegisterViewAsync(role, vm);
        }

        var result = await _accounts.RegisterAsync(vm, role);
        if (!result.Ok)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Message ?? "The account could not be created.");
            return await RegisterViewAsync(role, vm);
        }

        TempData["Success"] = "Your account has been created. Sign in to continue.";
        return RedirectToAction(nameof(Login));
    }
}
