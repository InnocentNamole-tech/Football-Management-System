using FootballClubManagement.Data;
using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Services;

public sealed class StatisticsService
{
    private readonly StatisticsRepository _statistics;
    private readonly MatchRepository _matches;
    private readonly PlayerRepository _players;

    public StatisticsService(StatisticsRepository statistics, MatchRepository matches, PlayerRepository players)
    {
        _statistics = statistics;
        _matches = matches;
        _players = players;
    }

    public async Task<ServiceResult> AddAsync(StatEntryVm vm, int clubId)
    {
        var match = await _matches.GetAsync(vm.MatchId, clubId);
        if (match is null) return ServiceResult.Fail("That match does not belong to your club.", nameof(vm.MatchId));

        var belongs = await _players.BelongsToClubAsync(vm.PlayerId, clubId);
        if (!belongs) return ServiceResult.Fail("That player is not active for your club.", nameof(vm.PlayerId));

        try
        {
            await _statistics.AddAsync(vm.MatchId, vm.PlayerId, vm.Goal, vm.Assist, vm.YellowCard, vm.RedCard, vm.MinutesPlayed);
            return ServiceResult.Success(vm.MatchId);
        }
        catch (SqlException ex) when (SqlErrors.IsUniqueViolation(ex))
        {
            return ServiceResult.Fail("Statistics are already recorded for that player in this match.", nameof(vm.PlayerId));
        }
    }

    public async Task<List<StatLineRow>?> ListForMatchAsync(int matchId, int clubId)
    {
        var match = await _matches.GetAsync(matchId, clubId);
        if (match is null) return null;
        return await _statistics.ListByMatchAsync(matchId, clubId);
    }

    public async Task<(List<PlayerTotalsRow> Totals, List<LeaderRow> Scorers, List<LeaderRow> Assists,
        List<LeaderRow> Yellow, List<LeaderRow> Red)> GetLeaderboardsAsync(int clubId)
    {
        var totals = await _players.GetClubTotalsAsync(clubId);
        var scorers = await _players.GetTopAsync(clubId, "Goals");
        var assists = await _players.GetTopAsync(clubId, "Assists");
        var yellow = await _players.GetTopAsync(clubId, "Yellow");
        var red = await _players.GetTopAsync(clubId, "Red");
        return (totals, scorers, assists, yellow, red);
    }

    public async Task<(PlayerOverview? Player, List<PlayerMatchStatRow> Matches, PlayerTotalsRow? Totals)> GetOwnAsync(int personId)
    {
        var player = await _players.GetByPersonAsync(personId);
        if (player is null) return (null, new List<PlayerMatchStatRow>(), null);

        var matches = await _statistics.ListForPlayerAsync(player.PlayerId);

        var totals = new PlayerTotalsRow
        {
            PlayerId = player.PlayerId,
            FirstName = player.FirstName,
            LastName = player.LastName,
            Nickname = player.Nickname,
            JerseyNumber = player.JerseyNumber,
            PositionName = player.PositionName,
            IsActive = player.IsActive,
            ProfilePhotoPath = player.ProfilePhotoPath,
            MatchesPlayed = matches.Count,
            Goals = matches.Sum(m => m.Goal),
            Assists = matches.Sum(m => m.Assist),
            YellowCards = matches.Sum(m => m.YellowCard),
            RedCards = matches.Sum(m => m.RedCard),
            MinutesPlayed = matches.Sum(m => m.MinutesPlayed)
        };

        return (player, matches, totals);
    }
}
