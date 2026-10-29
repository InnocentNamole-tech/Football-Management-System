using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FootballClubManagement.Controllers;

[Authorize]
public sealed class StatisticsController : AppController
{
    private readonly StatisticsService _statistics;
    private readonly MatchService _matches;
    private readonly PlayerService _players;

    public StatisticsController(StatisticsService statistics, MatchService matches, PlayerService players)
    {
        _statistics = statistics;
        _matches = matches;
        _players = players;
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        return View(await _matches.ListAsync(clubId, "results"));
    }

    [HttpGet]
    public async Task<IActionResult> Leaderboards()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var (totals, scorers, assists, yellow, red) = await _statistics.GetLeaderboardsAsync(clubId);
        ViewBag.Totals = totals;
        ViewBag.TopScorers = scorers;
        ViewBag.TopAssists = assists;
        ViewBag.YellowCards = yellow;
        ViewBag.RedCards = red;
        return View();
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> View(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var match = await _matches.GetAsync(id, clubId);
        if (match is null) return NotFound();

        ViewBag.Match = match;
        return View(await _statistics.ListForMatchAsync(id, clubId) ?? new List<StatLineRow>());
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpGet]
    public async Task<IActionResult> Add(int? matchId)
    {
        await PopulateLookupsAsync();
        return View(new StatEntryVm { MatchId = matchId ?? 0 });
    }

    [Authorize(Roles = RoleNames.Coordinator)]
    [HttpPost]
    public async Task<IActionResult> Add(StatEntryVm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync();
            return View(vm);
        }

        var result = await _statistics.AddAsync(vm, clubId);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateLookupsAsync();
            return View(vm);
        }

        FlashSuccess("The player statistics were added.");
        return RedirectToAction(nameof(View), new { id = vm.MatchId });
    }

    [Authorize(Roles = RoleNames.Player)]
    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        var (player, matches, totals) = await _statistics.GetOwnAsync(CurrentPersonId);
        if (player is null) return NotFound();

        ViewBag.Player = player;
        ViewBag.Totals = totals;
        return View(matches);
    }

    private async Task PopulateLookupsAsync()
    {
        var clubId = CurrentClubId ?? 0;

        ViewBag.Matches = new SelectList(await _matches.ListAsync(clubId, "results"), "MatchId", "Opponent");
        ViewBag.Players = new SelectList(await _players.ListAsync(clubId, includeInactive: false), "PlayerId", "FullName");
    }
}
