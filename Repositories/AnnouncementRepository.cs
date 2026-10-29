using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class AnnouncementRepository
{
    private readonly Db _db;

    public AnnouncementRepository(Db db) => _db = db;

    private const string SelectSql = @"
        SELECT a.AnnouncementId, a.ClubId, cl.ClubName, a.CoordinatorId,
               pe.FirstName + ' ' + pe.LastName AS AuthorName,
               a.Title, a.Description, a.DatePosted
        FROM dbo.Announcements a
        JOIN dbo.FootballClubs cl ON cl.ClubId = a.ClubId
        JOIN dbo.Coordinators co ON co.CoordinatorId = a.CoordinatorId
        JOIN dbo.Persons pe ON pe.PersonId = co.PersonId";

    public async Task<List<AnnouncementRow>> ListByClubAsync(int clubId)
    {
        const string sql = SelectSql + @" WHERE a.ClubId = @clubId ORDER BY a.DatePosted DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<AnnouncementRow>();
        while (await reader.ReadAsync()) rows.Add(Map(reader));
        return rows;
    }

    public async Task<AnnouncementRow?> GetAsync(int announcementId, int clubId)
    {
        const string sql = SelectSql + @" WHERE a.AnnouncementId = @id AND a.ClubId = @clubId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@id", announcementId));
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<int> CreateAsync(int coordinatorId, int clubId, string title, string description)
    {
        const string sql = @"
            INSERT INTO dbo.Announcements (CoordinatorId, ClubId, Title, Description)
            OUTPUT INSERTED.AnnouncementId
            VALUES (@coordinatorId, @clubId, @title, @description);";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@coordinatorId", coordinatorId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        command.Parameters.Add(Db.P("@title", title.Trim()));
        command.Parameters.Add(Db.P("@description", description.Trim()));
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> UpdateAsync(int announcementId, int clubId, string title, string description)
    {
        const string sql = @"
            UPDATE dbo.Announcements
            SET Title = @title, Description = @description
            WHERE AnnouncementId = @id AND ClubId = @clubId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@title", title.Trim()));
        command.Parameters.Add(Db.P("@description", description.Trim()));
        command.Parameters.Add(Db.P("@id", announcementId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int announcementId, int coordinatorId, int clubId)
    {
        const string sql = @"
            DELETE FROM dbo.Announcements
            WHERE AnnouncementId = @id AND CoordinatorId = @coordinatorId AND ClubId = @clubId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@id", announcementId));
        command.Parameters.Add(Db.P("@coordinatorId", coordinatorId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static AnnouncementRow Map(SqlDataReader reader) => new()
    {
        AnnouncementId = reader.Int("AnnouncementId"),
        ClubId = reader.Int("ClubId"),
        ClubName = reader.Str("ClubName"),
        CoordinatorId = reader.Int("CoordinatorId"),
        AuthorName = reader.Str("AuthorName"),
        Title = reader.Str("Title"),
        Description = reader.Str("Description"),
        DatePosted = reader.Date("DatePosted")
    };
}
