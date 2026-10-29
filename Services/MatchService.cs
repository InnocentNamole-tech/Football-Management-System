using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;

namespace FootballClubManagement.Services;

public sealed class MatchService
{
    private readonly MatchRepository _matches;
    private readonly CompetitionRepository _competitions;

    public MatchService(MatchRepository matches, CompetitionRepository competitions)
    {
        _matches = matches;
        _competitions = competitions;
    }

    public Task<List<MatchRow>> ListAsync(int clubId, string mode = "all") => _matches.ListByClubAsync(clubId, mode);

    public Task<MatchRow?> GetAsync(int matchId, int clubId) => _matches.GetAsync(matchId, clubId);

    public Task<MatchRow?> GetNextFixtureAsync(int clubId) => _matches.GetNextFixtureAsync(clubId);

    public Task<List<MatchRow>> GetRecentResultsAsync(int clubId, int take = 5) => _matches.GetRecentResultsAsync(clubId, take);

    public async Task<ServiceResult> CreateAsync(MatchCreateVm vm, int clubId)
    {
        if (vm.CompetitionId.HasValue)
        {
            var approved = await _competitions.IsApprovedAsync(vm.CompetitionId.Value, clubId);
            if (!approved)
            {
                return ServiceResult.Fail("Your club is not approved in that competition.", nameof(vm.CompetitionId));
            }
        }

        var matchId = await _matches.CreateAsync(
            clubId, vm.CompetitionId, vm.Opponent, vm.MatchDate, vm.KickoffTime.ToTimeSpan(), vm.StadiumId);

        return ServiceResult.Success(matchId);
    }

    public async Task<ServiceResult> SetResultAsync(MatchResultVm vm, int clubId)
    {
        if (vm.Result is not ("Win" or "Loss" or "Draw"))
        {
            return ServiceResult.Fail("Choose Win, Loss or Draw.", nameof(vm.Result));
        }

        var match = await _matches.GetAsync(vm.MatchId, clubId);
        if (match is null) return ServiceResult.Fail("That match does not belong to your club.");

        var updated = await _matches.UpdateResultAsync(vm.MatchId, clubId, vm.Result, vm.GoalFor, vm.GoalAgainst);
        return updated
            ? ServiceResult.Success(vm.MatchId)
            : ServiceResult.Fail("That match could not be updated.");
    }

    public async Task<MatchResultVm?> GetResultVmAsync(int matchId, int clubId)
    {
        var match = await _matches.GetAsync(matchId, clubId);
        if (match is null) return null;

        return new MatchResultVm
        {
            MatchId = match.MatchId,
            Opponent = match.Opponent,
            MatchDate = match.MatchDate,
            Result = match.Result ?? "",
            GoalFor = match.GoalFor ?? 0,
            GoalAgainst = match.GoalAgainst ?? 0
        };
    }
}
