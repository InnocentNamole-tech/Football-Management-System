using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class PlayerRepository
{
    private readonly Db _db;

    public PlayerRepository(Db db) => _db = db;

    private const string OverviewSelectSql = @"
        SELECT PlayerId, PersonId, FirstName, LastName, Email, Username, Phone, ProfilePhotoPath,
               Nickname, PlayerClubId, ClubId, ClubName, PositionId, PositionName, JerseyNumber,
               IsActive, DateJoined, PlayerStyleId, PlayerStyleName, CityId, CityName, ProvinceName
        FROM dbo.PlayerOverviews";

    public async Task<List<PlayerOverview>> ListByClubAsync(int clubId, bool includeInactive = false)
    {
        const string sql = OverviewSelectSql + @"
            WHERE ClubId = @clubId AND (@includeInactive = 1 OR IsActive = 1)
            ORDER BY IsActive DESC, JerseyNumber;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));
        command.Parameters.Add(Db.P("@includeInactive", includeInactive ? 1 : 0));

        await using var reader = await command.ExecuteReaderAsync();
        var players = new List<PlayerOverview>();
        while (await reader.ReadAsync()) players.Add(Map(reader));
        return players;
    }

    public async Task<PlayerOverview?> GetAsync(int playerId, int? clubId = null)
    {
        const string sql = OverviewSelectSql + @"
            WHERE PlayerId = @playerId AND (@clubId IS NULL OR ClubId = @clubId)
            ORDER BY IsActive DESC, DateJoined DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@playerId", playerId));
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<PlayerOverview?> GetByPersonAsync(int personId)
    {
        const string sql = OverviewSelectSql + @"
            WHERE PersonId = @personId
            ORDER BY IsActive DESC, DateJoined DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<int?> GetPlayerIdByPersonAsync(int personId)
    {
        const string sql = "SELECT PlayerId FROM dbo.Players WHERE PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? null : (int)result;
    }

    public async Task<bool> BelongsToClubAsync(int playerId, int clubId)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.PlayerClubs WHERE PlayerId = @playerId AND ClubId = @clubId AND IsActive = 1;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@playerId", playerId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        return (int)(await command.ExecuteScalarAsync() ?? 0) > 0;
    }

    public async Task<int> CreateAsync(
        string firstName, string lastName, string email, string username, string phone, string passwordHash,
        string nickname, int cityId, int? playerStyleId, int clubId, int positionId, int jerseyNumber, DateTime dateJoined)
    {
        const string personSql = @"
            INSERT INTO dbo.Persons (FirstName, LastName, Email, Username, Phone, PasswordHash)
            OUTPUT INSERTED.PersonId
            VALUES (@firstName, @lastName, @email, @username, @phone, @hash);";

        const string playerSql = @"
            INSERT INTO dbo.Players (PersonId, Nickname, CityId, PlayerStyleId)
            OUTPUT INSERTED.PlayerId
            VALUES (@personId, @nickname, @cityId, @styleId);";

        const string clubSql = @"
            INSERT INTO dbo.PlayerClubs (PlayerId, ClubId, PositionId, JerseyNumber, DateJoined, IsActive)
            VALUES (@playerId, @clubId, @positionId, @jersey, @joined, 1);";

        await using var connection = await _db.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            int personId;
            await using (var command = new SqlCommand(personSql, connection, transaction))
            {
                command.Parameters.Add(Db.P("@firstName", firstName.Trim()));
                command.Parameters.Add(Db.P("@lastName", lastName.Trim()));
                command.Parameters.Add(Db.P("@email", email.Trim()));
                command.Parameters.Add(Db.P("@username", username.Trim()));
                command.Parameters.Add(Db.P("@phone", phone.Trim()));
                command.Parameters.Add(Db.P("@hash", passwordHash));
                personId = (int)(await command.ExecuteScalarAsync() ?? 0);
            }

            int playerId;
            await using (var command = new SqlCommand(playerSql, connection, transaction))
            {
                command.Parameters.Add(Db.P("@personId", personId));
                command.Parameters.Add(Db.P("@nickname", nickname.Trim()));
                command.Parameters.Add(Db.P("@cityId", cityId));
                command.Parameters.Add(Db.P("@styleId", playerStyleId));
                playerId = (int)(await command.ExecuteScalarAsync() ?? 0);
            }

            await using (var command = new SqlCommand(clubSql, connection, transaction))
            {
                command.Parameters.Add(Db.P("@playerId", playerId));
                command.Parameters.Add(Db.P("@clubId", clubId));
                command.Parameters.Add(Db.P("@positionId", positionId));
                command.Parameters.Add(Db.P("@jersey", jerseyNumber));
                command.Parameters.Add(Db.P("@joined", dateJoined));
                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return playerId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(
        int playerId, int clubId, string firstName, string lastName, string email, string phone,
        string nickname, int cityId, int? playerStyleId, int positionId, int jerseyNumber)
    {
        const string personSql = @"
            UPDATE p SET p.FirstName = @firstName, p.LastName = @lastName, p.Email = @email, p.Phone = @phone
            FROM dbo.Persons p
            JOIN dbo.Players pl ON pl.PersonId = p.PersonId
            WHERE pl.PlayerId = @playerId;";

        const string playerSql = @"
            UPDATE dbo.Players
            SET Nickname = @nickname, CityId = @cityId, PlayerStyleId = @styleId
            WHERE PlayerId = @playerId;";

        const string clubSql = @"
            UPDATE dbo.PlayerClubs
            SET PositionId = @positionId, JerseyNumber = @jersey
            WHERE PlayerId = @playerId AND ClubId = @clubId AND IsActive = 1;";

        await using var connection = await _db.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            await using (var command = new SqlCommand(personSql, connection, transaction))
            {
                command.Parameters.Add(Db.P("@firstName", firstName.Trim()));
                command.Parameters.Add(Db.P("@lastName", lastName.Trim()));
                command.Parameters.Add(Db.P("@email", email.Trim()));
                command.Parameters.Add(Db.P("@phone", phone.Trim()));
                command.Parameters.Add(Db.P("@playerId", playerId));
                await command.ExecuteNonQueryAsync();
            }

            await using (var command = new SqlCommand(playerSql, connection, transaction))
            {
                command.Parameters.Add(Db.P("@nickname", nickname.Trim()));
                command.Parameters.Add(Db.P("@cityId", cityId));
                command.Parameters.Add(Db.P("@styleId", playerStyleId));
                command.Parameters.Add(Db.P("@playerId", playerId));
                await command.ExecuteNonQueryAsync();
            }

            int affected;
            await using (var command = new SqlCommand(clubSql, connection, transaction))
            {
                command.Parameters.Add(Db.P("@positionId", positionId));
                command.Parameters.Add(Db.P("@jersey", jerseyNumber));
                command.Parameters.Add(Db.P("@playerId", playerId));
                command.Parameters.Add(Db.P("@clubId", clubId));
                affected = await command.ExecuteNonQueryAsync();
            }

            if (affected == 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeactivateAsync(int playerId, int clubId)
    {
        const string sql = @"
            UPDATE dbo.PlayerClubs SET IsActive = 0
            WHERE PlayerId = @playerId AND ClubId = @clubId AND IsActive = 1;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@playerId", playerId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<PlayerTotalsRow>> GetClubTotalsAsync(int clubId, bool activeOnly = true)
    {
        const string sql = @"
            SELECT po.PlayerId, po.FirstName, po.LastName, po.Nickname, po.JerseyNumber, po.PositionName,
                   po.IsActive, po.ProfilePhotoPath,
                   COALESCE(pt.MatchesPlayed, 0) AS MatchesPlayed,
                   COALESCE(pt.Goals, 0) AS Goals,
                   COALESCE(pt.Assists, 0) AS Assists,
                   COALESCE(pt.YellowCards, 0) AS YellowCards,
                   COALESCE(pt.RedCards, 0) AS RedCards,
                   COALESCE(pt.MinutesPlayed, 0) AS MinutesPlayed
            FROM dbo.PlayerOverviews po
            LEFT JOIN dbo.PlayerTotals pt ON pt.PlayerId = po.PlayerId
            WHERE po.ClubId = @clubId AND (@activeOnly = 0 OR po.IsActive = 1)
            ORDER BY COALESCE(pt.Goals, 0) DESC, COALESCE(pt.Assists, 0) DESC, po.JerseyNumber;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));
        command.Parameters.Add(Db.P("@activeOnly", activeOnly ? 1 : 0));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<PlayerTotalsRow>();
        while (await reader.ReadAsync())
        {
            rows.Add(new PlayerTotalsRow
            {
                PlayerId = reader.Int("PlayerId"),
                FirstName = reader.Str("FirstName"),
                LastName = reader.Str("LastName"),
                Nickname = reader.Str("Nickname"),
                JerseyNumber = reader.Int("JerseyNumber"),
                PositionName = reader.Str("PositionName"),
                IsActive = reader.Bool("IsActive"),
                ProfilePhotoPath = reader.NStr("ProfilePhotoPath"),
                MatchesPlayed = reader.Int("MatchesPlayed"),
                Goals = reader.Int("Goals"),
                Assists = reader.Int("Assists"),
                YellowCards = reader.Int("YellowCards"),
                RedCards = reader.Int("RedCards"),
                MinutesPlayed = reader.Int("MinutesPlayed")
            });
        }

        return rows;
    }

    public async Task<List<LeaderRow>> GetTopAsync(int clubId, string metric, int take = 5)
    {
        var orderBy = metric switch
        {
            "Assists" => "COALESCE(pt.Assists, 0) DESC, COALESCE(pt.Goals, 0) DESC",
            "Yellow" => "COALESCE(pt.YellowCards, 0) DESC",
            "Red" => "COALESCE(pt.RedCards, 0) DESC",
            _ => "COALESCE(pt.Goals, 0) DESC, COALESCE(pt.Assists, 0) DESC"
        };

        var valueColumn = metric switch
        {
            "Assists" => "COALESCE(pt.Assists, 0)",
            "Yellow" => "COALESCE(pt.YellowCards, 0)",
            "Red" => "COALESCE(pt.RedCards, 0)",
            _ => "COALESCE(pt.Goals, 0)"
        };

        var sql = $@"
            SELECT TOP (@take) po.PlayerId, po.FirstName + ' ' + po.LastName AS FullName,
                   po.JerseyNumber, po.PositionName, {valueColumn} AS Value
            FROM dbo.PlayerOverviews po
            LEFT JOIN dbo.PlayerTotals pt ON pt.PlayerId = po.PlayerId
            WHERE po.ClubId = @clubId AND po.IsActive = 1 AND {valueColumn} > 0
            ORDER BY {orderBy}, po.JerseyNumber;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));
        command.Parameters.Add(Db.P("@take", take));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<LeaderRow>();
        while (await reader.ReadAsync())
        {
            rows.Add(new LeaderRow
            {
                PlayerId = reader.Int("PlayerId"),
                FullName = reader.Str("FullName"),
                JerseyNumber = reader.Int("JerseyNumber"),
                PositionName = reader.Str("PositionName"),
                Value = reader.Int("Value")
            });
        }

        return rows;
    }

    private static PlayerOverview Map(SqlDataReader reader) => new()
    {
        PlayerId = reader.Int("PlayerId"),
        PersonId = reader.Int("PersonId"),
        FirstName = reader.Str("FirstName"),
        LastName = reader.Str("LastName"),
        Email = reader.Str("Email"),
        Username = reader.Str("Username"),
        Phone = reader.Str("Phone"),
        ProfilePhotoPath = reader.NStr("ProfilePhotoPath"),
        Nickname = reader.Str("Nickname"),
        PlayerClubId = reader.Int("PlayerClubId"),
        ClubId = reader.Int("ClubId"),
        ClubName = reader.Str("ClubName"),
        PositionId = reader.Int("PositionId"),
        PositionName = reader.Str("PositionName"),
        JerseyNumber = reader.Int("JerseyNumber"),
        IsActive = reader.Bool("IsActive"),
        DateJoined = reader.Date("DateJoined"),
        PlayerStyleId = reader.NInt("PlayerStyleId"),
        PlayerStyleName = reader.NStr("PlayerStyleName"),
        CityId = reader.Int("CityId"),
        CityName = reader.Str("CityName"),
        ProvinceName = reader.Str("ProvinceName")
    };
}
