using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;

namespace FootballClubManagement.Services;

public sealed class SquadService
{
    private readonly SquadRepository _squads;
    private readonly PlayerRepository _players;
    private readonly MatchRepository _matches;

    public SquadService(SquadRepository squads, PlayerRepository players, MatchRepository matches)
    {
        _squads = squads;
        _players = players;
        _matches = matches;
    }

    public async Task<List<SquadOption>> GetOptionsAsync(int clubId)
    {
        var players = await _players.ListByClubAsync(clubId, includeInactive: false);
        return players.Select(p => new SquadOption
        {
            PlayerId = p.PlayerId,
            FullName = p.FullName,
            JerseyNumber = p.JerseyNumber,
            PositionName = p.PositionName
        }).ToList();
    }

    public async Task<string> GetMatchLabelAsync(int matchId, int clubId)
    {
        var match = await _matches.GetAsync(matchId, clubId);
        return match is null ? "Unknown match" : $"{match.MatchDate:dd MMM yyyy} vs {match.Opponent}";
    }

    public Task<List<SquadPlayerRow>> GetSquadAsync(int matchId, int clubId) => _squads.GetSquadAsync(matchId, clubId);

    public Task<List<SquadSummaryRow>> GetSummariesAsync(int clubId) => _squads.GetSummariesAsync(clubId);

    public async Task<ServiceResult> SaveAsync(SquadDraft draft, int clubId)
    {
        var match = await _matches.GetAsync(draft.MatchId, clubId);
        if (match is null) return ServiceResult.Fail("That match does not belong to your club.", nameof(draft.MatchId));

        if (!Formations.All.Contains(draft.Formation))
        {
            return ServiceResult.Fail("Choose a formation for the squad.");
        }

        if (draft.Starters.Count != 11)
        {
            return ServiceResult.Fail("A starting eleven needs exactly 11 players.");
        }

        if (draft.Bench.Count > 8)
        {
            return ServiceResult.Fail("The bench can hold at most 8 players.");
        }

        var allIds = draft.Starters.Concat(draft.Bench).ToList();
        if (allIds.Count != allIds.Distinct().Count())
        {
            return ServiceResult.Fail("A player cannot appear more than once in the squad.");
        }

        var roster = await _players.ListByClubAsync(clubId, includeInactive: false);
        var rosterIds = roster.Select(p => p.PlayerId).ToHashSet();

        if (allIds.Any(id => !rosterIds.Contains(id)))
        {
            return ServiceResult.Fail("Only active players from your club can be selected.");
        }

        await _squads.SaveAsync(draft.MatchId, clubId, draft.Formation, draft.Starters, draft.Bench);
        return ServiceResult.Success(draft.MatchId);
    }
}
