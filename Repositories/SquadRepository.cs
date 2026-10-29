using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class SquadRepository
{
    private readonly Db _db;

    public SquadRepository(Db db) => _db = db;

    public async Task<List<SquadPlayerRow>> GetSquadAsync(int matchId, int clubId)
    {
        const string sql = @"
            SELECT PlayerId, FirstName, LastName, JerseyNumber, PositionName, IsStarting
            FROM dbo.SquadLineups
            WHERE MatchId = @matchId AND ClubId = @clubId
            ORDER BY IsStarting DESC, JerseyNumber;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@matchId", matchId));
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<SquadPlayerRow>();
        while (await reader.ReadAsync())
        {
            rows.Add(new SquadPlayerRow
            {
                PlayerId = reader.Int("PlayerId"),
                FirstName = reader.Str("FirstName"),
                LastName = reader.Str("LastName"),
                JerseyNumber = reader.Int("JerseyNumber"),
                PositionName = reader.Str("PositionName"),
                IsStarting = reader.Bool("IsStarting")
            });
        }

        return rows;
    }

    public async Task<List<SquadSummaryRow>> GetSummariesAsync(int clubId)
    {
        const string sql = @"
            SELECT m.MatchId, m.Opponent, m.MatchDate, m.Formation,
                   SUM(CASE WHEN s.IsStarting = 1 THEN 1 ELSE 0 END) AS Starters,
                   SUM(CASE WHEN s.IsStarting = 0 THEN 1 ELSE 0 END) AS Bench
            FROM dbo.Matches m
            JOIN dbo.Squads s ON s.MatchId = m.MatchId
            WHERE m.ClubId = @clubId
            GROUP BY m.MatchId, m.Opponent, m.MatchDate, m.Formation
            ORDER BY m.MatchDate DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<SquadSummaryRow>();
        while (await reader.ReadAsync())
        {
            rows.Add(new SquadSummaryRow
            {
                MatchId = reader.Int("MatchId"),
                Opponent = reader.Str("Opponent"),
                MatchDate = reader.Date("MatchDate"),
                Formation = reader.NStr("Formation"),
                Starters = reader.Int("Starters"),
                Bench = reader.Int("Bench")
            });
        }

        return rows;
    }

    public async Task<bool> HasSquadAsync(int matchId, int clubId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM dbo.Squads s
            JOIN dbo.Matches m ON m.MatchId = s.MatchId
            WHERE s.MatchId = @matchId AND m.ClubId = @clubId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@matchId", matchId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        return (int)(await command.ExecuteScalarAsync() ?? 0) > 0;
    }

    public async Task SaveAsync(int matchId, int clubId, string formation, IReadOnlyList<int> starters, IReadOnlyList<int> bench)
    {
        const string verifySql = "SELECT COUNT(*) FROM dbo.Matches WHERE MatchId = @matchId AND ClubId = @clubId;";
        const string deleteSql = "DELETE FROM dbo.Squads WHERE MatchId = @matchId;";
        const string insertSql = @"
            INSERT INTO dbo.Squads (MatchId, PlayerId, IsStarting)
            VALUES (@matchId, @playerId, @isStarting);";
        const string formationSql = "UPDATE dbo.Matches SET Formation = @formation WHERE MatchId = @matchId AND ClubId = @clubId;";

        await using var connection = await _db.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            await using (var command = new SqlCommand(verifySql, connection, transaction))
            {
                command.Parameters.Add(Db.P("@matchId", matchId));
                command.Parameters.Add(Db.P("@clubId", clubId));
                if ((int)(await command.ExecuteScalarAsync() ?? 0) != 1)
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException("Match not found for this club.");
                }
            }

            await using (var command = new SqlCommand(deleteSql, connection, transaction))
            {
                command.Parameters.Add(Db.P("@matchId", matchId));
                await command.ExecuteNonQueryAsync();
            }

            foreach (var (playerId, isStarting) in starters.Select(id => (id, true)).Concat(bench.Select(id => (id, false))))
            {
                await using var command = new SqlCommand(insertSql, connection, transaction);
                command.Parameters.Add(Db.P("@matchId", matchId));
                command.Parameters.Add(Db.P("@playerId", playerId));
                command.Parameters.Add(Db.P("@isStarting", isStarting));
                await command.ExecuteNonQueryAsync();
            }

            await using (var command = new SqlCommand(formationSql, connection, transaction))
            {
                command.Parameters.Add(Db.P("@formation", formation));
                command.Parameters.Add(Db.P("@matchId", matchId));
                command.Parameters.Add(Db.P("@clubId", clubId));
                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
