using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class UnavailabilityRepository
{
    private readonly Db _db;

    public UnavailabilityRepository(Db db) => _db = db;

    private const string SelectSql = @"
        SELECT u.UnavailabilityId, u.PlayerId,
               pe.FirstName + ' ' + pe.LastName AS PlayerName,
               COALESCE((SELECT TOP 1 pc.JerseyNumber FROM dbo.PlayerClubs pc
                         WHERE pc.PlayerId = u.PlayerId ORDER BY pc.IsActive DESC, pc.DateJoined DESC), 0) AS JerseyNumber,
               COALESCE((SELECT TOP 1 pc.ClubId FROM dbo.PlayerClubs pc
                         WHERE pc.PlayerId = u.PlayerId ORDER BY pc.IsActive DESC, pc.DateJoined DESC), 0) AS ClubId,
               u.StartDate, u.EndDate, u.Reason
        FROM dbo.Unavailabilities u
        JOIN dbo.Players pl ON pl.PlayerId = u.PlayerId
        JOIN dbo.Persons pe ON pe.PersonId = pl.PersonId";

    public async Task<List<UnavailabilityRow>> ListByClubAsync(int clubId)
    {
        const string sql = @"
            SELECT u.UnavailabilityId, u.PlayerId,
                   pe.FirstName + ' ' + pe.LastName AS PlayerName,
                   pc.JerseyNumber, pc.ClubId, u.StartDate, u.EndDate, u.Reason
            FROM dbo.Unavailabilities u
            JOIN dbo.Players pl ON pl.PlayerId = u.PlayerId
            JOIN dbo.Persons pe ON pe.PersonId = pl.PersonId
            JOIN dbo.PlayerClubs pc ON pc.PlayerId = u.PlayerId AND pc.IsActive = 1
            WHERE pc.ClubId = @clubId
            ORDER BY u.StartDate DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<UnavailabilityRow>();
        while (await reader.ReadAsync()) rows.Add(Map(reader));
        return rows;
    }

    public async Task<List<UnavailabilityRow>> ListByPlayerAsync(int playerId)
    {
        const string sql = SelectSql + @"
            WHERE u.PlayerId = @playerId
            ORDER BY u.StartDate DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@playerId", playerId));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<UnavailabilityRow>();
        while (await reader.ReadAsync()) rows.Add(Map(reader));
        return rows;
    }

    public async Task<UnavailabilityRow?> GetForPlayerAsync(int unavailabilityId, int playerId)
    {
        const string sql = SelectSql + @" WHERE u.UnavailabilityId = @id AND u.PlayerId = @playerId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@id", unavailabilityId));
        command.Parameters.Add(Db.P("@playerId", playerId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<int> CreateAsync(int playerId, DateTime startDate, DateTime endDate, string reason)
    {
        const string sql = @"
            INSERT INTO dbo.Unavailabilities (PlayerId, StartDate, EndDate, Reason)
            OUTPUT INSERTED.UnavailabilityId
            VALUES (@playerId, @start, @end, @reason);";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@playerId", playerId));
        command.Parameters.Add(Db.P("@start", startDate));
        command.Parameters.Add(Db.P("@end", endDate));
        command.Parameters.Add(Db.P("@reason", reason.Trim()));
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> UpdateAsync(int unavailabilityId, int playerId, DateTime startDate, DateTime endDate, string reason)
    {
        const string sql = @"
            UPDATE dbo.Unavailabilities
            SET StartDate = @start, EndDate = @end, Reason = @reason
            WHERE UnavailabilityId = @id AND PlayerId = @playerId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@start", startDate));
        command.Parameters.Add(Db.P("@end", endDate));
        command.Parameters.Add(Db.P("@reason", reason.Trim()));
        command.Parameters.Add(Db.P("@id", unavailabilityId));
        command.Parameters.Add(Db.P("@playerId", playerId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int unavailabilityId, int playerId)
    {
        const string sql = @"
            DELETE FROM dbo.Unavailabilities
            WHERE UnavailabilityId = @id AND PlayerId = @playerId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@id", unavailabilityId));
        command.Parameters.Add(Db.P("@playerId", playerId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static UnavailabilityRow Map(SqlDataReader reader) => new()
    {
        UnavailabilityId = reader.Int("UnavailabilityId"),
        PlayerId = reader.Int("PlayerId"),
        PlayerName = reader.Str("PlayerName"),
        JerseyNumber = reader.Int("JerseyNumber"),
        ClubId = reader.Int("ClubId"),
        StartDate = reader.Date("StartDate"),
        EndDate = reader.Date("EndDate"),
        Reason = reader.Str("Reason")
    };
}
