using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class CommentRepository
{
    private readonly Db _db;

    public CommentRepository(Db db) => _db = db;

    private const string SelectSql = @"
        SELECT cm.CommentId, cm.MatchId, m.ClubId, cl.ClubName, m.Opponent, m.MatchDate,
               cm.PersonId, pe.FirstName + ' ' + pe.LastName AS AuthorName,
               cm.CommentText, cm.PostedAt
        FROM dbo.Comments cm
        JOIN dbo.Matches m ON m.MatchId = cm.MatchId
        JOIN dbo.FootballClubs cl ON cl.ClubId = m.ClubId
        JOIN dbo.Persons pe ON pe.PersonId = cm.PersonId";

    public async Task<List<CommentRow>> ListByClubAsync(int clubId)
    {
        const string sql = SelectSql + @" WHERE m.ClubId = @clubId ORDER BY cm.PostedAt DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<CommentRow>();
        while (await reader.ReadAsync()) rows.Add(Map(reader));
        return rows;
    }

    public async Task<List<CommentRow>> ListByMatchAsync(int matchId, int clubId)
    {
        const string sql = SelectSql + @" WHERE cm.MatchId = @matchId AND m.ClubId = @clubId ORDER BY cm.PostedAt DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@matchId", matchId));
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<CommentRow>();
        while (await reader.ReadAsync()) rows.Add(Map(reader));
        return rows;
    }

    public async Task<CommentRow?> GetAsync(int commentId)
    {
        const string sql = SelectSql + @" WHERE cm.CommentId = @id;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@id", commentId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<int> CreateAsync(int matchId, int personId, string commentText)
    {
        const string sql = @"
            INSERT INTO dbo.Comments (MatchId, PersonId, CommentText)
            OUTPUT INSERTED.CommentId
            VALUES (@matchId, @personId, @text);";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@matchId", matchId));
        command.Parameters.Add(Db.P("@personId", personId));
        command.Parameters.Add(Db.P("@text", commentText.Trim()));
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> UpdateAsync(int commentId, int personId, string commentText)
    {
        const string sql = @"
            UPDATE dbo.Comments SET CommentText = @text
            WHERE CommentId = @id AND PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@text", commentText.Trim()));
        command.Parameters.Add(Db.P("@id", commentId));
        command.Parameters.Add(Db.P("@personId", personId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int commentId, int personId)
    {
        const string sql = @"
            DELETE FROM dbo.Comments
            WHERE CommentId = @id AND PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@id", commentId));
        command.Parameters.Add(Db.P("@personId", personId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static CommentRow Map(SqlDataReader reader) => new()
    {
        CommentId = reader.Int("CommentId"),
        MatchId = reader.Int("MatchId"),
        ClubId = reader.Int("ClubId"),
        ClubName = reader.Str("ClubName"),
        Opponent = reader.Str("Opponent"),
        MatchDate = reader.Date("MatchDate"),
        PersonId = reader.Int("PersonId"),
        AuthorName = reader.Str("AuthorName"),
        CommentText = reader.Str("CommentText"),
        PostedAt = reader.Date("PostedAt")
    };
}
