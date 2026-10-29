using System.Text.Json;
using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballClubManagement.Controllers;

[Authorize(Roles = RoleNames.Coordinator + "," + RoleNames.Coach + "," + RoleNames.Player + "," + RoleNames.Fan)]
public sealed class SquadsController : AppController
{
    private const string DraftKey = "SquadDraft";

    private readonly SquadService _squads;
    private readonly MatchService _matches;

    public SquadsController(SquadService squads, MatchService matches)
    {
        _squads = squads;
        _matches = matches;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        ViewBag.CanManageSquad = CurrentRole == RoleNames.Coach;
        return View(await _squads.GetSummariesAsync(clubId));
    }

    [Authorize(Roles = RoleNames.Coach)]
    [HttpGet]
    public async Task<IActionResult> Select()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        ViewBag.Matches = await _matches.ListAsync(clubId);
        ViewBag.Formations = Formations.All;
        return View(new SquadStep1Vm());
    }

    [Authorize(Roles = RoleNames.Coach)]
    [HttpPost]
    public async Task<IActionResult> Select(SquadStep1Vm vm)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var match = await _matches.GetAsync(vm.MatchId, clubId);
        if (match is null)
        {
            ModelState.AddModelError(nameof(vm.MatchId), "Choose a match from your club.");
        }

        if (!Formations.All.Contains(vm.Formation))
        {
            ModelState.AddModelError(nameof(vm.Formation), "Choose a formation.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Matches = await _matches.ListAsync(clubId);
            ViewBag.Formations = Formations.All;
            return View(vm);
        }

        SaveDraft(new SquadDraft { MatchId = vm.MatchId, Formation = vm.Formation });
        return RedirectToAction(nameof(Step2));
    }

    [Authorize(Roles = RoleNames.Coach)]
    [HttpGet]
    public async Task<IActionResult> Step2()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var draft = LoadDraft();
        if (draft is null) return RedirectToAction(nameof(Select));

        var options = await _squads.GetOptionsAsync(clubId);
        return View(new SquadSelectionVm
        {
            MatchId = draft.MatchId,
            Formation = draft.Formation,
            MatchLabel = await _squads.GetMatchLabelAsync(draft.MatchId, clubId),
            StepTitle = "Choose the starting eleven",
            HelpText = "Tick exactly 11 players. Only active players from your club are listed.",
            Options = options,
            Selected = draft.Starters
        });
    }

    [Authorize(Roles = RoleNames.Coach)]
    [HttpPost]
    public async Task<IActionResult> Step2(List<int>? selected)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var draft = LoadDraft();
        if (draft is null) return RedirectToAction(nameof(Select));

        var ids = selected ?? new List<int>();
        var options = await _squads.GetOptionsAsync(clubId);
        var validIds = options.Select(o => o.PlayerId).ToHashSet();

        if (ids.Count != 11)
        {
            ModelState.AddModelError(string.Empty, "A starting eleven needs exactly 11 players.");
        }
        else if (ids.Distinct().Count() != 11 || ids.Any(id => !validIds.Contains(id)))
        {
            ModelState.AddModelError(string.Empty, "Choose 11 different active players from your club.");
        }

        if (!ModelState.IsValid)
        {
            return View(new SquadSelectionVm
            {
                MatchId = draft.MatchId,
                Formation = draft.Formation,
                MatchLabel = await _squads.GetMatchLabelAsync(draft.MatchId, clubId),
                StepTitle = "Choose the starting eleven",
                HelpText = "Tick exactly 11 players. Only active players from your club are listed.",
                Options = options,
                Selected = ids
            });
        }

        draft.Starters = ids;
        draft.Bench = draft.Bench.Where(id => !ids.Contains(id)).ToList();
        SaveDraft(draft);
        return RedirectToAction(nameof(Step3));
    }

    [Authorize(Roles = RoleNames.Coach)]
    [HttpGet]
    public async Task<IActionResult> Step3()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var draft = LoadDraft();
        if (draft is null) return RedirectToAction(nameof(Select));

        var options = (await _squads.GetOptionsAsync(clubId))
            .Where(o => !draft.Starters.Contains(o.PlayerId))
            .ToList();

        return View(new SquadSelectionVm
        {
            MatchId = draft.MatchId,
            Formation = draft.Formation,
            MatchLabel = await _squads.GetMatchLabelAsync(draft.MatchId, clubId),
            StepTitle = "Pick the bench",
            HelpText = "Choose up to 8 substitutes. Starters are already taken.",
            Options = options,
            Selected = draft.Bench
        });
    }

    [Authorize(Roles = RoleNames.Coach)]
    [HttpPost]
    public async Task<IActionResult> Step3(List<int>? selected)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var draft = LoadDraft();
        if (draft is null) return RedirectToAction(nameof(Select));

        var ids = selected ?? new List<int>();
        var options = (await _squads.GetOptionsAsync(clubId))
            .Where(o => !draft.Starters.Contains(o.PlayerId))
            .ToList();
        var validIds = options.Select(o => o.PlayerId).ToHashSet();

        if (ids.Count > 8)
        {
            ModelState.AddModelError(string.Empty, "The bench can hold at most 8 players.");
        }
        else if (ids.Distinct().Count() != ids.Count || ids.Any(id => !validIds.Contains(id)))
        {
            ModelState.AddModelError(string.Empty, "Choose substitutes from the listed players only.");
        }

        if (!ModelState.IsValid)
        {
            return View(new SquadSelectionVm
            {
                MatchId = draft.MatchId,
                Formation = draft.Formation,
                MatchLabel = await _squads.GetMatchLabelAsync(draft.MatchId, clubId),
                StepTitle = "Pick the bench",
                HelpText = "Choose up to 8 substitutes. Starters are already taken.",
                Options = options,
                Selected = ids
            });
        }

        draft.Bench = ids;
        SaveDraft(draft);
        return RedirectToAction(nameof(Review));
    }

    [Authorize(Roles = RoleNames.Coach)]
    [HttpGet]
    public async Task<IActionResult> Review()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var draft = LoadDraft();
        if (draft is null) return RedirectToAction(nameof(Select));

        var options = (await _squads.GetOptionsAsync(clubId)).ToDictionary(o => o.PlayerId);
        var starters = draft.Starters.Where(options.ContainsKey).Select(id => options[id]).ToList();
        var bench = draft.Bench.Where(options.ContainsKey).Select(id => options[id]).ToList();

        return View(new SquadReviewVm
        {
            MatchId = draft.MatchId,
            MatchLabel = await _squads.GetMatchLabelAsync(draft.MatchId, clubId),
            Formation = draft.Formation,
            Starters = starters,
            Bench = bench
        });
    }

    [Authorize(Roles = RoleNames.Coach)]
    [HttpPost]
    public async Task<IActionResult> Confirm()
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var draft = LoadDraft();
        if (draft is null)
        {
            FlashError("Your squad draft expired. Start the selection again.");
            return RedirectToAction(nameof(Select));
        }

        var result = await _squads.SaveAsync(draft, clubId);
        if (!result.Ok)
        {
            FlashError(result.Message ?? "The squad could not be saved.");
            return RedirectToAction(nameof(Review));
        }

        HttpContext.Session.Remove(DraftKey);
        FlashSuccess("The matchday squad has been saved.");
        return RedirectToAction(nameof(Details), new { id = draft.MatchId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var match = await _matches.GetAsync(id, clubId);
        if (match is null) return NotFound();

        ViewBag.Match = match;
        ViewBag.CanManageSquad = CurrentRole == RoleNames.Coach;
        return View(await _squads.GetSquadAsync(id, clubId));
    }

    [Authorize(Roles = RoleNames.Coach)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!TryCurrentClub(out var clubId)) return AccessDeniedPage();

        var match = await _matches.GetAsync(id, clubId);
        if (match is null) return NotFound();

        var existing = await _squads.GetSquadAsync(id, clubId);
        var draft = new SquadDraft
        {
            MatchId = id,
            Formation = string.IsNullOrWhiteSpace(match.Formation) ? Formations.All[0] : match.Formation,
            Starters = existing.Where(p => p.IsStarting).Select(p => p.PlayerId).ToList(),
            Bench = existing.Where(p => !p.IsStarting).Select(p => p.PlayerId).ToList()
        };

        SaveDraft(draft);
        return RedirectToAction(nameof(Step2));
    }

    private void SaveDraft(SquadDraft draft) =>
        HttpContext.Session.SetString(DraftKey, JsonSerializer.Serialize(draft));

    private SquadDraft? LoadDraft()
    {
        var json = HttpContext.Session.GetString(DraftKey);
        return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<SquadDraft>(json);
    }
}
