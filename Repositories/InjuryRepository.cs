using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class InjuryRepository
{
    private readonly Db _db;

    public InjuryRepository(Db db) => _db = db;

    private const string SelectSql = @"
        SELECT ih.InjuryHistoryId, ih.PlayerId,
               pe.FirstName + ' ' + pe.LastName AS PlayerName,
               pc.JerseyNumber, pc.ClubId,
               ih.InjuryType, ih.InjuryDate, ih.EstimatedRecovery, ih.ActualRecovery
        FROM dbo.InjuryHistories ih
        JOIN dbo.Players pl ON pl.PlayerId = ih.PlayerId
        JOIN dbo.Persons pe ON pe.PersonId = pl.PersonId
        JOIN dbo.PlayerClubs pc ON pc.PlayerId = ih.PlayerId AND pc.IsActive = 1
        JOIN dbo.FootballClubs cl ON cl.ClubId = pc.ClubId";

    public async Task<List<InjuryRow>> ListByClubAsync(int clubId)
    {
        const string sql = SelectSql + @"
            WHERE pc.ClubId = @clubId
            ORDER BY CASE WHEN ih.ActualRecovery IS NULL THEN 0 ELSE 1 END, ih.InjuryDate DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<InjuryRow>();
        while (await reader.ReadAsync()) rows.Add(Map(reader));
        return rows;
    }

    public async Task<List<InjuryRow>> ListByPlayerAsync(int playerId)
    {
        const string sql = @"
            SELECT ih.InjuryHistoryId, ih.PlayerId,
                   pe.FirstName + ' ' + pe.LastName AS PlayerName,
                   COALESCE((SELECT TOP 1 pc.JerseyNumber FROM dbo.PlayerClubs pc
                             WHERE pc.PlayerId = ih.PlayerId ORDER BY pc.IsActive DESC, pc.DateJoined DESC), 0) AS JerseyNumber,
                   COALESCE((SELECT TOP 1 pc.ClubId FROM dbo.PlayerClubs pc
                             WHERE pc.PlayerId = ih.PlayerId ORDER BY pc.IsActive DESC, pc.DateJoined DESC), 0) AS ClubId,
                   ih.InjuryType, ih.InjuryDate, ih.EstimatedRecovery, ih.ActualRecovery
            FROM dbo.InjuryHistories ih
            JOIN dbo.Players pl ON pl.PlayerId = ih.PlayerId
            JOIN dbo.Persons pe ON pe.PersonId = pl.PersonId
            WHERE ih.PlayerId = @playerId
            ORDER BY CASE WHEN ih.ActualRecovery IS NULL THEN 0 ELSE 1 END, ih.InjuryDate DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@playerId", playerId));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<InjuryRow>();
        while (await reader.ReadAsync()) rows.Add(Map(reader));
        return rows;
    }

    public async Task<InjuryRow?> GetAsync(int injuryId, int clubId)
    {
        const string sql = SelectSql + @" WHERE ih.InjuryHistoryId = @injuryId AND pc.ClubId = @clubId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@injuryId", injuryId));
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<int> CreateAsync(int playerId, string injuryType, DateTime injuryDate, string estimatedRecovery)
    {
        const string sql = @"
            INSERT INTO dbo.InjuryHistories (PlayerId, InjuryType, InjuryDate, EstimatedRecovery)
            OUTPUT INSERTED.InjuryHistoryId
            VALUES (@playerId, @injuryType, @injuryDate, @estimated);";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@playerId", playerId));
        command.Parameters.Add(Db.P("@injuryType", injuryType.Trim()));
        command.Parameters.Add(Db.P("@injuryDate", injuryDate));
        command.Parameters.Add(Db.P("@estimated", estimatedRecovery.Trim()));
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> UpdateAsync(int injuryId, int clubId, string injuryType, DateTime injuryDate,
        string estimatedRecovery, string? actualRecovery)
    {
        const string sql = @"
            UPDATE ih
            SET ih.InjuryType = @injuryType,
                ih.InjuryDate = @injuryDate,
                ih.EstimatedRecovery = @estimated,
                ih.ActualRecovery = @actual
            FROM dbo.InjuryHistories ih
            JOIN dbo.PlayerClubs pc ON pc.PlayerId = ih.PlayerId AND pc.IsActive = 1
            WHERE ih.InjuryHistoryId = @injuryId AND pc.ClubId = @clubId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@injuryType", injuryType.Trim()));
        command.Parameters.Add(Db.P("@injuryDate", injuryDate));
        command.Parameters.Add(Db.P("@estimated", estimatedRecovery.Trim()));
        command.Parameters.Add(Db.P("@actual", string.IsNullOrWhiteSpace(actualRecovery) ? null : actualRecovery.Trim()));
        command.Parameters.Add(Db.P("@injuryId", injuryId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static InjuryRow Map(SqlDataReader reader) => new()
    {
        InjuryHistoryId = reader.Int("InjuryHistoryId"),
        PlayerId = reader.Int("PlayerId"),
        PlayerName = reader.Str("PlayerName"),
        JerseyNumber = reader.Int("JerseyNumber"),
        ClubId = reader.Int("ClubId"),
        InjuryType = reader.Str("InjuryType"),
        InjuryDate = reader.Date("InjuryDate"),
        EstimatedRecovery = reader.Str("EstimatedRecovery"),
        ActualRecovery = reader.NStr("ActualRecovery")
    };
}
