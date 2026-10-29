using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class TeamHistoryRepository
{
    private readonly Db _db;

    public TeamHistoryRepository(Db db) => _db = db;

    private const string SelectSql = @"
        SELECT h.HistoryId, h.CoordinatorId, h.ClubId, cl.ClubName,
               pe.FirstName + ' ' + pe.LastName AS AuthorName,
               h.Title, h.Description, h.[Year]
        FROM dbo.TeamHistories h
        JOIN dbo.Coordinators co ON co.CoordinatorId = h.CoordinatorId
        JOIN dbo.Persons pe ON pe.PersonId = co.PersonId
        JOIN dbo.FootballClubs cl ON cl.ClubId = h.ClubId";

    public async Task<List<TeamHistoryRow>> ListByClubAsync(int clubId)
    {
        const string sql = SelectSql + @" WHERE h.ClubId = @clubId ORDER BY h.[Year] DESC, h.HistoryId DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<TeamHistoryRow>();
        while (await reader.ReadAsync()) rows.Add(Map(reader));
        return rows;
    }

    public async Task<TeamHistoryRow?> GetAsync(int historyId, int clubId)
    {
        const string sql = SelectSql + @" WHERE h.HistoryId = @id AND h.ClubId = @clubId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@id", historyId));
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<int> CreateAsync(int coordinatorId, int clubId, string title, string description, int year)
    {
        const string sql = @"
            INSERT INTO dbo.TeamHistories (CoordinatorId, ClubId, Title, Description, [Year])
            OUTPUT INSERTED.HistoryId
            VALUES (@coordinatorId, @clubId, @title, @description, @year);";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@coordinatorId", coordinatorId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        command.Parameters.Add(Db.P("@title", title.Trim()));
        command.Parameters.Add(Db.P("@description", description.Trim()));
        command.Parameters.Add(Db.P("@year", year));
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> UpdateAsync(int historyId, int clubId, string title, string description, int year)
    {
        const string sql = @"
            UPDATE dbo.TeamHistories
            SET Title = @title, Description = @description, [Year] = @year
            WHERE HistoryId = @id AND ClubId = @clubId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@title", title.Trim()));
        command.Parameters.Add(Db.P("@description", description.Trim()));
        command.Parameters.Add(Db.P("@year", year));
        command.Parameters.Add(Db.P("@id", historyId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int historyId, int clubId)
    {
        const string sql = @"DELETE FROM dbo.TeamHistories WHERE HistoryId = @id AND ClubId = @clubId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@id", historyId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static TeamHistoryRow Map(SqlDataReader reader) => new()
    {
        HistoryId = reader.Int("HistoryId"),
        CoordinatorId = reader.Int("CoordinatorId"),
        ClubId = reader.Int("ClubId"),
        ClubName = reader.Str("ClubName"),
        AuthorName = reader.Str("AuthorName"),
        Title = reader.Str("Title"),
        Description = reader.Str("Description"),
        Year = reader.Int("Year")
    };
}
