using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class MatchRepository
{
    private readonly Db _db;

    public MatchRepository(Db db) => _db = db;

    private const string MatchSelectSql = @"
        SELECT m.MatchId, m.ClubId, cl.ClubName,
               m.CompetitionId, co.CompetitionName, co.CompetitionType,
               m.Opponent, m.MatchDate, m.KickoffTime, m.StadiumId, st.StadiumName,
               m.Formation, m.Result, m.GoalFor, m.GoalAgainst,
               CASE WHEN EXISTS (SELECT 1 FROM dbo.Squads s WHERE s.MatchId = m.MatchId)
                    THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasSquad,
               (SELECT COUNT(*) FROM dbo.Comments cm WHERE cm.MatchId = m.MatchId) AS CommentCount
        FROM dbo.Matches m
        JOIN dbo.FootballClubs cl ON cl.ClubId = m.ClubId
        LEFT JOIN dbo.Competitions co ON co.CompetitionId = m.CompetitionId
        JOIN dbo.Stadiums st ON st.StadiumId = m.StadiumId";

    public async Task<List<MatchRow>> ListByClubAsync(int clubId, string mode = "all")
    {
        var filter = mode switch
        {
            "fixtures" => "AND m.Result IS NULL ORDER BY m.MatchDate ASC, m.KickoffTime ASC",
            "results" => "AND m.Result IS NOT NULL ORDER BY m.MatchDate DESC, m.KickoffTime DESC",
            _ => "ORDER BY CASE WHEN m.Result IS NULL THEN 0 ELSE 1 END, m.MatchDate DESC"
        };

        var sql = MatchSelectSql + " WHERE m.ClubId = @clubId " + filter + ";";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var matches = new List<MatchRow>();
        while (await reader.ReadAsync()) matches.Add(Map(reader));
        return matches;
    }

    public async Task<MatchRow?> GetAsync(int matchId, int? clubId = null)
    {
        const string sql = MatchSelectSql + @"
            WHERE m.MatchId = @matchId AND (@clubId IS NULL OR m.ClubId = @clubId);";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@matchId", matchId));
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<int> CreateAsync(int clubId, int? competitionId, string opponent, DateTime matchDate,
        TimeSpan kickoffTime, int stadiumId)
    {
        const string sql = @"
            INSERT INTO dbo.Matches (ClubId, CompetitionId, Opponent, MatchDate, KickoffTime, StadiumId)
            OUTPUT INSERTED.MatchId
            VALUES (@clubId, @competitionId, @opponent, @matchDate, @kickoffTime, @stadiumId);";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));
        command.Parameters.Add(Db.P("@competitionId", competitionId));
        command.Parameters.Add(Db.P("@opponent", opponent.Trim()));
        command.Parameters.Add(Db.P("@matchDate", matchDate));
        command.Parameters.Add(Db.P("@kickoffTime", kickoffTime));
        command.Parameters.Add(Db.P("@stadiumId", stadiumId));
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> UpdateResultAsync(int matchId, int clubId, string result, int goalFor, int goalAgainst)
    {
        const string sql = @"
            UPDATE dbo.Matches
            SET Result = @result, GoalFor = @goalFor, GoalAgainst = @goalAgainst
            WHERE MatchId = @matchId AND ClubId = @clubId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@result", result));
        command.Parameters.Add(Db.P("@goalFor", goalFor));
        command.Parameters.Add(Db.P("@goalAgainst", goalAgainst));
        command.Parameters.Add(Db.P("@matchId", matchId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<LookupItem>> GetCompletedMatchOptionsAsync(int clubId)
    {
        const string sql = @"
            SELECT MatchId AS Id,
                   CONVERT(VARCHAR(11), MatchDate, 106) + ' vs ' + Opponent AS Name
            FROM dbo.Matches
            WHERE ClubId = @clubId AND Result IS NOT NULL
            ORDER BY MatchDate DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var items = new List<LookupItem>();
        while (await reader.ReadAsync())
        {
            items.Add(new LookupItem { Id = reader.Int("Id"), Name = reader.Str("Name") });
        }

        return items;
    }

    public async Task<List<LookupItem>> GetMatchOptionsAsync(int clubId)
    {
        const string sql = @"
            SELECT MatchId AS Id,
                   CONVERT(VARCHAR(11), MatchDate, 106) + ' vs ' + Opponent AS Name
            FROM dbo.Matches
            WHERE ClubId = @clubId
            ORDER BY MatchDate DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var items = new List<LookupItem>();
        while (await reader.ReadAsync())
        {
            items.Add(new LookupItem { Id = reader.Int("Id"), Name = reader.Str("Name") });
        }

        return items;
    }

    public async Task<MatchRow?> GetNextFixtureAsync(int clubId)
    {
        const string sql = MatchSelectSql + @"
            WHERE m.ClubId = @clubId AND m.Result IS NULL
            ORDER BY m.MatchDate ASC, m.KickoffTime ASC
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<List<MatchRow>> GetRecentResultsAsync(int clubId, int take = 5)
    {
        const string sql = MatchSelectSql + @"
            WHERE m.ClubId = @clubId AND m.Result IS NOT NULL
            ORDER BY m.MatchDate DESC, m.KickoffTime DESC
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));
        command.Parameters.Add(Db.P("@take", take));

        await using var reader = await command.ExecuteReaderAsync();
        var matches = new List<MatchRow>();
        while (await reader.ReadAsync()) matches.Add(Map(reader));
        return matches;
    }

    public async Task<List<MatchRow>> GetUpcomingAsync(int clubId, int take = 3)
    {
        const string sql = MatchSelectSql + @"
            WHERE m.ClubId = @clubId AND m.Result IS NULL
            ORDER BY m.MatchDate ASC, m.KickoffTime ASC
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));
        command.Parameters.Add(Db.P("@take", take));

        await using var reader = await command.ExecuteReaderAsync();
        var matches = new List<MatchRow>();
        while (await reader.ReadAsync()) matches.Add(Map(reader));
        return matches;
    }

    public async Task<bool> BelongsToClubAsync(int matchId, int clubId)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.Matches WHERE MatchId = @matchId AND ClubId = @clubId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@matchId", matchId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        return (int)(await command.ExecuteScalarAsync() ?? 0) > 0;
    }

    private static MatchRow Map(SqlDataReader reader) => new()
    {
        MatchId = reader.Int("MatchId"),
        ClubId = reader.Int("ClubId"),
        ClubName = reader.Str("ClubName"),
        CompetitionId = reader.NInt("CompetitionId"),
        CompetitionName = reader.NStr("CompetitionName"),
        CompetitionType = reader.NStr("CompetitionType"),
        Opponent = reader.Str("Opponent"),
        MatchDate = reader.Date("MatchDate"),
        KickoffTime = reader.TimeOfDay("KickoffTime"),
        StadiumId = reader.Int("StadiumId"),
        StadiumName = reader.Str("StadiumName"),
        Formation = reader.NStr("Formation"),
        Result = reader.NStr("Result"),
        GoalFor = reader.NInt("GoalFor"),
        GoalAgainst = reader.NInt("GoalAgainst"),
        HasSquad = reader.Bool("HasSquad"),
        CommentCount = reader.Int("CommentCount")
    };
}
