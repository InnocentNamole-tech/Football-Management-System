using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FootballClubManagement.Controllers;

[Authorize]
public sealed class CompetitionsController : AppController
{
    private readonly CompetitionService _competitions;
    private readonly LookupService _lookups;

    public CompetitionsController(CompetitionService competitions, LookupService lookups)
    {
        _competitions = competitions;
        _lookups = lookups;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (CurrentRole == RoleNames.Organiser)
        {
            return View(new CompetitionsIndexVm
            {
                Role = CurrentRole,
                Competitions = await _competitions.ListForOrganiserAsync(CurrentPersonId),
                PendingCount = await _competitions.PendingCountAsync(CurrentPersonId)
            });
        }

        if (CurrentRole == RoleNames.Admin)
        {
            return View(new CompetitionsIndexVm { Role = CurrentRole });
        }

        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var club = await _lookups.ClubAsync(clubId);
        var vm = new CompetitionsIndexVm
        {
            Role = CurrentRole,
            ClubName = club?.ClubName
        };

        if (CurrentRole == RoleNames.Coach)
        {
            vm = new CompetitionsIndexVm
            {
                Role = CurrentRole,
                ClubName = club?.ClubName,
                Entries = await _competitions.ListEntriesAsync(clubId, "Approved")
            };
        }
        else
        {
            vm = new CompetitionsIndexVm
            {
                Role = CurrentRole,
                ClubName = club?.ClubName,
                Competitions = await _competitions.MatchingForClubAsync(clubId),
                Entries = await _competitions.ListEntriesAsync(clubId)
            };
        }

        return View(vm);
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> History()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();
        return View(await _competitions.ListEntriesAsync(clubId));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (CurrentRole == RoleNames.Organiser)
        {
            var mine = await _competitions.ListForOrganiserAsync(CurrentPersonId);
            var competition = mine.FirstOrDefault(c => c.CompetitionId == id);
            if (competition is null) return NotFound();

            ViewBag.CanEdit = true;
            return View(competition);
        }

        if (CurrentRole == RoleNames.Admin)
        {
            var any = await _competitions.GetAsync(id);
            if (any is null) return NotFound();
            return View(any);
        }

        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var row = await _competitions.GetForClubAsync(id, clubId);
        if (row is null) return NotFound();

        var club = await _lookups.ClubAsync(clubId);
        ViewBag.CanRequest = CurrentRole == RoleNames.Coordinator
            && row.EntryId is null
            && row.AgeGroup == club?.AgeGroup
            && (row.Gender == club?.Gender || row.Gender == "Mixed");

        return View(row);
    }

    [Authorize(Roles = RoleNames.Organiser)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateStadiumsAsync();
        return View(new CompetitionVm());
    }

    [Authorize(Roles = RoleNames.Organiser)]
    [HttpPost]
    public async Task<IActionResult> Create(CompetitionVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateStadiumsAsync();
            return View(vm);
        }

        var result = await _competitions.CreateAsync(vm, CurrentPersonId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateStadiumsAsync();
            return View(vm);
        }

        FlashSuccess("The competition has been created.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Organiser)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _competitions.GetEditVmAsync(id, CurrentPersonId);
        if (vm is null) return NotFound();

        await PopulateStadiumsAsync();
        return View(vm);
    }

    [Authorize(Roles = RoleNames.Organiser)]
    [HttpPost]
    public async Task<IActionResult> Edit(CompetitionVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateStadiumsAsync();
            return View(vm);
        }

        var result = await _competitions.UpdateAsync(vm, CurrentPersonId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateStadiumsAsync();
            return View(vm);
        }

        FlashSuccess("The competition has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> RequestToJoin(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var result = await _competitions.RequestToJoinAsync(id, clubId);
        if (result.Ok)
        {
            FlashSuccess("Your request to join has been sent to the organiser.");
        }
        else
        {
            FlashError(result.Message ?? "That request could not be sent.");
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Organiser)]
    [HttpGet]
    public async Task<IActionResult> Requests(string? status)
    {
        ViewData["Status"] = status;
        return View(await _competitions.RequestsAsync(CurrentPersonId, status));
    }

    [Authorize(Roles = RoleNames.Organiser)]
    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _competitions.SetRequestStatusAsync(id, CurrentPersonId, "Approved");
        Flash(result, "The request has been approved.");
        return RedirectToAction(nameof(Requests));
    }

    [Authorize(Roles = RoleNames.Organiser)]
    [HttpPost]
    public async Task<IActionResult> Reject(int id)
    {
        var result = await _competitions.SetRequestStatusAsync(id, CurrentPersonId, "Rejected");
        Flash(result, "The request has been rejected.");
        return RedirectToAction(nameof(Requests));
    }

    [Authorize(Roles = RoleNames.Organiser)]
    [HttpGet]
    public async Task<IActionResult> Outcomes()
    {
        return View(await _competitions.ApprovedOutcomesAsync(CurrentPersonId));
    }

    [Authorize(Roles = RoleNames.Organiser)]
    [HttpGet]
    public async Task<IActionResult> Outcome(int id)
    {
        var vm = await _competitions.GetOutcomeVmAsync(id, CurrentPersonId);
        if (vm is null) return NotFound();

        ViewBag.Stages = Stages.All;
        return View(vm);
    }

    [Authorize(Roles = RoleNames.Organiser)]
    [HttpPost]
    public async Task<IActionResult> Outcome(OutcomeVm vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Stages = Stages.All;
            return View(vm);
        }

        var result = await _competitions.RecordOutcomeAsync(vm, CurrentPersonId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            ViewBag.Stages = Stages.All;
            return View(vm);
        }

        FlashSuccess("The outcome has been recorded.");
        return RedirectToAction(nameof(Outcomes));
    }

    private async Task PopulateStadiumsAsync()
    {
        ViewBag.Stadiums = new SelectList(await _lookups.StadiumsAsync(), "Id", "Name");
    }

    private void Flash(ServiceResult result, string success)
    {
        if (result.Ok)
        {
            FlashSuccess(success);
        }
        else
        {
            FlashError(result.Message ?? "That request could not be updated.");
        }
    }
}
