using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class StatisticsRepository
{
    private readonly Db _db;

    public StatisticsRepository(Db db) => _db = db;

    public async Task<List<StatLineRow>> ListByMatchAsync(int matchId, int clubId)
    {
        const string sql = @"
            SELECT ps.PlayerStatId, ps.MatchId, ps.PlayerId,
                   pe.FirstName + ' ' + pe.LastName AS PlayerName,
                   pc.JerseyNumber, ps.Goal, ps.Assist, ps.YellowCard, ps.RedCard, ps.MinutesPlayed
            FROM dbo.PlayerStats ps
            JOIN dbo.Matches m ON m.MatchId = ps.MatchId AND m.ClubId = @clubId
            JOIN dbo.Players pl ON pl.PlayerId = ps.PlayerId
            JOIN dbo.Persons pe ON pe.PersonId = pl.PersonId
            JOIN dbo.PlayerClubs pc ON pc.PlayerId = ps.PlayerId AND pc.ClubId = m.ClubId AND pc.IsActive = 1
            WHERE ps.MatchId = @matchId
            ORDER BY ps.Goal DESC, pc.JerseyNumber;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@matchId", matchId));
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<StatLineRow>();
        while (await reader.ReadAsync()) rows.Add(Map(reader));
        return rows;
    }

    public async Task<int> AddAsync(int matchId, int playerId, int goal, int assist, int yellowCard, int redCard, int minutesPlayed)
    {
        const string sql = @"
            INSERT INTO dbo.PlayerStats (MatchId, PlayerId, Goal, Assist, YellowCard, RedCard, MinutesPlayed)
            OUTPUT INSERTED.PlayerStatId
            VALUES (@matchId, @playerId, @goal, @assist, @yellow, @red, @minutes);";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@matchId", matchId));
        command.Parameters.Add(Db.P("@playerId", playerId));
        command.Parameters.Add(Db.P("@goal", goal));
        command.Parameters.Add(Db.P("@assist", assist));
        command.Parameters.Add(Db.P("@yellow", yellowCard));
        command.Parameters.Add(Db.P("@red", redCard));
        command.Parameters.Add(Db.P("@minutes", minutesPlayed));
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<List<PlayerMatchStatRow>> ListForPlayerAsync(int playerId)
    {
        const string sql = @"
            SELECT ps.PlayerStatId, ps.MatchId, m.MatchDate, m.Opponent, m.Result,
                   ps.Goal, ps.Assist, ps.YellowCard, ps.RedCard, ps.MinutesPlayed
            FROM dbo.PlayerStats ps
            JOIN dbo.Matches m ON m.MatchId = ps.MatchId
            WHERE ps.PlayerId = @playerId
            ORDER BY m.MatchDate DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@playerId", playerId));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<PlayerMatchStatRow>();
        while (await reader.ReadAsync())
        {
            rows.Add(new PlayerMatchStatRow
            {
                PlayerStatId = reader.Int("PlayerStatId"),
                MatchId = reader.Int("MatchId"),
                MatchDate = reader.Date("MatchDate"),
                Opponent = reader.Str("Opponent"),
                Result = reader.NStr("Result"),
                Goal = reader.Int("Goal"),
                Assist = reader.Int("Assist"),
                YellowCard = reader.Int("YellowCard"),
                RedCard = reader.Int("RedCard"),
                MinutesPlayed = reader.Int("MinutesPlayed")
            });
        }

        return rows;
    }

    public async Task<StatLineRow?> GetOwnTotalsAsync(int playerId)
    {
        const string sql = @"
            SELECT ps.MatchId, ps.PlayerId, '' AS PlayerName, 0 AS JerseyNumber,
                   SUM(ps.Goal) AS Goal, SUM(ps.Assist) AS Assist,
                   SUM(ps.YellowCard) AS YellowCard, SUM(ps.RedCard) AS RedCard,
                   SUM(ps.MinutesPlayed) AS MinutesPlayed, MIN(ps.PlayerStatId) AS PlayerStatId
            FROM dbo.PlayerStats ps
            WHERE ps.PlayerId = @playerId
            GROUP BY ps.MatchId, ps.PlayerId
            ORDER BY ps.MatchId DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@playerId", playerId));

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return Map(reader);
    }

    private static StatLineRow Map(SqlDataReader reader) => new()
    {
        PlayerStatId = reader.Int("PlayerStatId"),
        MatchId = reader.Int("MatchId"),
        PlayerId = reader.Int("PlayerId"),
        PlayerName = reader.Str("PlayerName"),
        JerseyNumber = reader.Int("JerseyNumber"),
        Goal = reader.Int("Goal"),
        Assist = reader.Int("Assist"),
        YellowCard = reader.Int("YellowCard"),
        RedCard = reader.Int("RedCard"),
        MinutesPlayed = reader.Int("MinutesPlayed")
    };
}
