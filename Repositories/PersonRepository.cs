using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class PersonRepository
{
    private readonly Db _db;

    public PersonRepository(Db db) => _db = db;

    private const string RoleCaseSql = @"
        CASE
            WHEN a.AdminId IS NOT NULL THEN 'Admin'
            WHEN o.OrganiserId IS NOT NULL THEN 'Organiser'
            WHEN co.CoordinatorId IS NOT NULL THEN 'Coordinator'
            WHEN ch.CoachId IS NOT NULL THEN 'Coach'
            WHEN pl.PlayerId IS NOT NULL THEN 'Player'
            WHEN f.FanId IS NOT NULL THEN 'Fan'
        END";

    private const string RoleJoinsSql = @"
        LEFT JOIN dbo.Admins a ON a.PersonId = p.PersonId
        LEFT JOIN dbo.Organisers o ON o.PersonId = p.PersonId
        LEFT JOIN dbo.Coordinators co ON co.PersonId = p.PersonId
        LEFT JOIN dbo.Coaches ch ON ch.PersonId = p.PersonId
        LEFT JOIN dbo.Players pl ON pl.PersonId = p.PersonId
        LEFT JOIN dbo.Fans f ON f.PersonId = p.PersonId";

    public async Task<AuthUser?> FindForLoginAsync(string username)
    {
        const string sql = @"
            SELECT p.PersonId, p.FirstName, p.LastName, p.Username, p.PasswordHash,
                   p.FailedLoginCount, p.LockoutEnd,
                   " + RoleCaseSql + @" AS Role,
                   CASE
                       WHEN a.AdminId IS NOT NULL OR o.OrganiserId IS NOT NULL THEN NULL
                       WHEN co.CoordinatorId IS NOT NULL THEN co.ClubId
                       WHEN ch.CoachId IS NOT NULL THEN
                           COALESCE((SELECT TOP 1 cc.ClubId FROM dbo.CoachClubs cc
                                     WHERE cc.CoachId = ch.CoachId ORDER BY cc.DateJoined DESC),
                                    (SELECT TOP 1 cc.ClubId FROM dbo.CoachClubs cc WHERE cc.CoachId = ch.CoachId))
                       WHEN pl.PlayerId IS NOT NULL THEN
                           COALESCE((SELECT TOP 1 pc.ClubId FROM dbo.PlayerClubs pc
                                     WHERE pc.PlayerId = pl.PlayerId AND pc.IsActive = 1 ORDER BY pc.DateJoined DESC),
                                    (SELECT TOP 1 pc.ClubId FROM dbo.PlayerClubs pc
                                     WHERE pc.PlayerId = pl.PlayerId ORDER BY pc.DateJoined DESC))
                       WHEN f.FanId IS NOT NULL THEN
                           (SELECT TOP 1 fc.ClubId FROM dbo.FanClubs fc
                            WHERE fc.FanId = f.FanId ORDER BY fc.DateJoined DESC)
                   END AS ClubId
            FROM dbo.Persons p
            " + RoleJoinsSql + @"
            WHERE p.Username COLLATE Latin1_General_CI_AS = @username;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@username", username.Trim()));

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new AuthUser
        {
            PersonId = reader.Int("PersonId"),
            FirstName = reader.Str("FirstName"),
            LastName = reader.Str("LastName"),
            Username = reader.Str("Username"),
            PasswordHash = reader.Str("PasswordHash"),
            FailedLoginCount = reader.Int("FailedLoginCount"),
            LockoutEnd = reader.NDate("LockoutEnd"),
            Role = reader.NStr("Role"),
            ClubId = reader.NInt("ClubId")
        };
    }

    public async Task<AuthUser?> FindForPasswordResetAsync(string email, string username)
    {
        const string sql = @"
            SELECT PersonId, FirstName, LastName, Username
            FROM dbo.Persons
            WHERE Email COLLATE Latin1_General_CI_AS = @email
              AND Username COLLATE Latin1_General_CI_AS = @username;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@email", email.Trim()));
        command.Parameters.Add(Db.P("@username", username.Trim()));

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new AuthUser
        {
            PersonId = reader.Int("PersonId"),
            FirstName = reader.Str("FirstName"),
            LastName = reader.Str("LastName"),
            Username = reader.Str("Username")
        };
    }

    public async Task RegisterFailedLoginAsync(int personId)
    {
        const string sql = @"
            UPDATE dbo.Persons
            SET FailedLoginCount = FailedLoginCount + 1,
                LockoutEnd = CASE WHEN FailedLoginCount + 1 >= 5
                                  THEN DATEADD(MINUTE, 5, SYSUTCDATETIME())
                                  ELSE NULL END
            WHERE PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));
        await command.ExecuteNonQueryAsync();
    }

    public async Task ResetLoginStateAsync(int personId)
    {
        const string sql = @"UPDATE dbo.Persons SET FailedLoginCount = 0, LockoutEnd = NULL WHERE PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Person?> GetAsync(int personId)
    {
        const string sql = @"
            SELECT PersonId, FirstName, LastName, Email, Username, Phone, ProfilePhotoPath, CreatedAt
            FROM dbo.Persons WHERE PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new Person
        {
            PersonId = reader.Int("PersonId"),
            FirstName = reader.Str("FirstName"),
            LastName = reader.Str("LastName"),
            Email = reader.Str("Email"),
            Username = reader.Str("Username"),
            Phone = reader.Str("Phone"),
            ProfilePhotoPath = reader.NStr("ProfilePhotoPath"),
            CreatedAt = reader.Date("CreatedAt")
        };
    }

    public async Task<(string? Role, string? ClubName)> GetRoleInfoAsync(int personId)
    {
        const string sql = @"
            SELECT TOP 1 " + RoleCaseSql + @" AS Role,
                   COALESCE(
                       (SELECT TOP 1 c.ClubName FROM dbo.Coordinators x
                        JOIN dbo.FootballClubs c ON c.ClubId = x.ClubId WHERE x.PersonId = p.PersonId),
                       (SELECT TOP 1 c.ClubName FROM dbo.Coaches x
                        JOIN dbo.CoachClubs cc ON cc.CoachId = x.CoachId
                        JOIN dbo.FootballClubs c ON c.ClubId = cc.ClubId
                        WHERE x.PersonId = p.PersonId ORDER BY cc.DateJoined DESC),
                       (SELECT TOP 1 c.ClubName FROM dbo.Players x
                        JOIN dbo.PlayerClubs pc ON pc.PlayerId = x.PlayerId
                        JOIN dbo.FootballClubs c ON c.ClubId = pc.ClubId
                        WHERE x.PersonId = p.PersonId ORDER BY pc.IsActive DESC, pc.DateJoined DESC),
                       (SELECT TOP 1 c.ClubName FROM dbo.Fans x
                        JOIN dbo.FanClubs fc ON fc.FanId = x.FanId
                        JOIN dbo.FootballClubs c ON c.ClubId = fc.ClubId
                        WHERE x.PersonId = p.PersonId ORDER BY fc.DateJoined DESC)
                   ) AS ClubName
            FROM dbo.Persons p
            " + RoleJoinsSql + @"
            WHERE p.PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return (null, null);

        return (reader.NStr("Role"), reader.NStr("ClubName"));
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludePersonId = null)
    {
        const string sql = @"SELECT COUNT(*) FROM dbo.Persons WHERE Email COLLATE Latin1_General_CI_AS = @email AND (@exclude IS NULL OR PersonId <> @exclude);";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@email", email.Trim()));
        command.Parameters.Add(Db.P("@exclude", excludePersonId));
        return (int)(await command.ExecuteScalarAsync() ?? 0) > 0;
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        const string sql = @"SELECT COUNT(*) FROM dbo.Persons WHERE Username COLLATE Latin1_General_CI_AS = @username;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@username", username.Trim()));
        return (int)(await command.ExecuteScalarAsync() ?? 0) > 0;
    }

    public async Task UpdateProfileAsync(int personId, string firstName, string lastName, string email, string phone, string? photoPath)
    {
        const string sql = @"
            UPDATE dbo.Persons
            SET FirstName = @firstName, LastName = @lastName, Email = @email, Phone = @phone,
                ProfilePhotoPath = COALESCE(@photoPath, ProfilePhotoPath)
            WHERE PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@firstName", firstName.Trim()));
        command.Parameters.Add(Db.P("@lastName", lastName.Trim()));
        command.Parameters.Add(Db.P("@email", email.Trim()));
        command.Parameters.Add(Db.P("@phone", phone.Trim()));
        command.Parameters.Add(Db.P("@photoPath", photoPath));
        command.Parameters.Add(Db.P("@personId", personId));
        await command.ExecuteNonQueryAsync();
    }

    public async Task<string?> GetPasswordHashAsync(int personId)
    {
        const string sql = "SELECT PasswordHash FROM dbo.Persons WHERE PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? reader.Str("PasswordHash") : null;
    }

    public async Task UpdatePasswordAsync(int personId, string passwordHash)
    {
        const string sql = @"
            UPDATE dbo.Persons
            SET PasswordHash = @hash, FailedLoginCount = 0, LockoutEnd = NULL
            WHERE PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@hash", passwordHash));
        command.Parameters.Add(Db.P("@personId", personId));
        await command.ExecuteNonQueryAsync();
    }

    public async Task UnlockAsync(int personId)
    {
        const string sql = @"UPDATE dbo.Persons SET FailedLoginCount = 0, LockoutEnd = NULL WHERE PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<AccountRow>> GetAccountsAsync(string? search)
    {
        const string sql = @"
            SELECT p.PersonId, p.FirstName, p.LastName, p.Email, p.Username,
                   p.FailedLoginCount, p.LockoutEnd, p.CreatedAt,
                   " + RoleCaseSql + @" AS Role,
                   COALESCE(
                       (SELECT TOP 1 c.ClubName FROM dbo.Coordinators x
                        JOIN dbo.FootballClubs c ON c.ClubId = x.ClubId WHERE x.PersonId = p.PersonId),
                       (SELECT TOP 1 c.ClubName FROM dbo.Coaches x
                        JOIN dbo.CoachClubs cc ON cc.CoachId = x.CoachId
                        JOIN dbo.FootballClubs c ON c.ClubId = cc.ClubId
                        WHERE x.PersonId = p.PersonId ORDER BY cc.DateJoined DESC),
                       (SELECT TOP 1 c.ClubName FROM dbo.Players x
                        JOIN dbo.PlayerClubs pc ON pc.PlayerId = x.PlayerId
                        JOIN dbo.FootballClubs c ON c.ClubId = pc.ClubId
                        WHERE x.PersonId = p.PersonId ORDER BY pc.IsActive DESC, pc.DateJoined DESC),
                       (SELECT TOP 1 c.ClubName FROM dbo.Fans x
                        JOIN dbo.FanClubs fc ON fc.FanId = x.FanId
                        JOIN dbo.FootballClubs c ON c.ClubId = fc.ClubId
                        WHERE x.PersonId = p.PersonId ORDER BY fc.DateJoined DESC)
                   ) AS ClubName
            FROM dbo.Persons p
            " + RoleJoinsSql + @"
            WHERE (@search IS NULL OR p.FirstName LIKE @like OR p.LastName LIKE @like
                   OR p.Username LIKE @like OR p.Email LIKE @like)
            ORDER BY p.LastName, p.FirstName;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        command.Parameters.Add(Db.P("@search", term));
        command.Parameters.Add(Db.P("@like", term is null ? null : $"%{term}%"));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<AccountRow>();
        while (await reader.ReadAsync())
        {
            rows.Add(new AccountRow
            {
                PersonId = reader.Int("PersonId"),
                FirstName = reader.Str("FirstName"),
                LastName = reader.Str("LastName"),
                Email = reader.Str("Email"),
                Username = reader.Str("Username"),
                Role = reader.NStr("Role"),
                ClubName = reader.NStr("ClubName"),
                FailedLoginCount = reader.Int("FailedLoginCount"),
                LockoutEnd = reader.NDate("LockoutEnd"),
                CreatedAt = reader.Date("CreatedAt")
            });
        }

        return rows;
    }

    public async Task<int> CreateAccountAsync(
        string firstName, string lastName, string email, string username, string phone, string passwordHash,
        string role, int? clubId, string? nickname, int? cityId, int? positionId, int? jerseyNumber,
        int? playerStyleId, int? specializationId, string? licenseLevel, DateTime? dateJoined)
    {
        const string personSql = @"
            INSERT INTO dbo.Persons (FirstName, LastName, Email, Username, Phone, PasswordHash)
            OUTPUT INSERTED.PersonId
            VALUES (@firstName, @lastName, @email, @username, @phone, @hash);";

        await using var connection = await _db.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            await using var personCommand = new SqlCommand(personSql, connection, transaction);
            personCommand.Parameters.Add(Db.P("@firstName", firstName.Trim()));
            personCommand.Parameters.Add(Db.P("@lastName", lastName.Trim()));
            personCommand.Parameters.Add(Db.P("@email", email.Trim()));
            personCommand.Parameters.Add(Db.P("@username", username.Trim()));
            personCommand.Parameters.Add(Db.P("@phone", phone.Trim()));
            personCommand.Parameters.Add(Db.P("@hash", passwordHash));
            var personId = (int)(await personCommand.ExecuteScalarAsync() ?? 0);

            switch (role)
            {
                case RoleNames.Admin:
                    await using (var roleCommand = new SqlCommand("INSERT INTO dbo.Admins (PersonId) VALUES (@id);", connection, transaction))
                    {
                        roleCommand.Parameters.Add(Db.P("@id", personId));
                        await roleCommand.ExecuteNonQueryAsync();
                    }
                    break;

                case RoleNames.Organiser:
                    await using (var roleCommand = new SqlCommand("INSERT INTO dbo.Organisers (PersonId) VALUES (@id);", connection, transaction))
                    {
                        roleCommand.Parameters.Add(Db.P("@id", personId));
                        await roleCommand.ExecuteNonQueryAsync();
                    }
                    break;

                case RoleNames.Coordinator:
                    await using (var roleCommand = new SqlCommand("INSERT INTO dbo.Coordinators (PersonId, ClubId) VALUES (@id, @clubId);", connection, transaction))
                    {
                        roleCommand.Parameters.Add(Db.P("@id", personId));
                        roleCommand.Parameters.Add(Db.P("@clubId", clubId));
                        await roleCommand.ExecuteNonQueryAsync();
                    }
                    break;

                case RoleNames.Coach:
                {
                    int coachId;
                    await using (var coachCommand = new SqlCommand(
                        "INSERT INTO dbo.Coaches (PersonId, LicenseLevel) OUTPUT INSERTED.CoachId VALUES (@id, @licenseLevel);", connection, transaction))
                    {
                        coachCommand.Parameters.Add(Db.P("@id", personId));
                        coachCommand.Parameters.Add(Db.P("@licenseLevel", licenseLevel ?? "SAFA C"));
                        coachId = (int)(await coachCommand.ExecuteScalarAsync() ?? 0);
                    }

                    await using var linkCommand = new SqlCommand(
                        "INSERT INTO dbo.CoachClubs (CoachId, ClubId, SpecializationId, DateJoined) VALUES (@id, @clubId, @specId, @joined);",
                        connection, transaction);
                    linkCommand.Parameters.Add(Db.P("@id", coachId));
                    linkCommand.Parameters.Add(Db.P("@clubId", clubId));
                    linkCommand.Parameters.Add(Db.P("@specId", specializationId));
                    linkCommand.Parameters.Add(Db.P("@joined", dateJoined ?? DateTime.Today));
                    await linkCommand.ExecuteNonQueryAsync();
                    break;
                }

                case RoleNames.Player:
                {
                    int playerId;
                    await using (var playerCommand = new SqlCommand(
                        "INSERT INTO dbo.Players (PersonId, Nickname, CityId, PlayerStyleId) OUTPUT INSERTED.PlayerId VALUES (@id, @nickname, @cityId, @styleId);",
                        connection, transaction))
                    {
                        playerCommand.Parameters.Add(Db.P("@id", personId));
                        playerCommand.Parameters.Add(Db.P("@nickname", nickname));
                        playerCommand.Parameters.Add(Db.P("@cityId", cityId));
                        playerCommand.Parameters.Add(Db.P("@styleId", playerStyleId));
                        playerId = (int)(await playerCommand.ExecuteScalarAsync() ?? 0);
                    }

                    await using var linkCommand = new SqlCommand(
                        "INSERT INTO dbo.PlayerClubs (PlayerId, ClubId, PositionId, JerseyNumber, DateJoined, IsActive) VALUES (@id, @clubId, @positionId, @jersey, @joined, 1);",
                        connection, transaction);
                    linkCommand.Parameters.Add(Db.P("@id", playerId));
                    linkCommand.Parameters.Add(Db.P("@clubId", clubId));
                    linkCommand.Parameters.Add(Db.P("@positionId", positionId));
                    linkCommand.Parameters.Add(Db.P("@jersey", jerseyNumber));
                    linkCommand.Parameters.Add(Db.P("@joined", dateJoined ?? DateTime.Today));
                    await linkCommand.ExecuteNonQueryAsync();
                    break;
                }

                case RoleNames.Fan:
                {
                    int fanId;
                    await using (var fanCommand = new SqlCommand(
                        "INSERT INTO dbo.Fans (PersonId) OUTPUT INSERTED.FanId VALUES (@id);", connection, transaction))
                    {
                        fanCommand.Parameters.Add(Db.P("@id", personId));
                        fanId = (int)(await fanCommand.ExecuteScalarAsync() ?? 0);
                    }

                    await using var linkCommand = new SqlCommand(
                        "INSERT INTO dbo.FanClubs (FanId, ClubId, DateJoined) VALUES (@id, @clubId, @joined);",
                        connection, transaction);
                    linkCommand.Parameters.Add(Db.P("@id", fanId));
                    linkCommand.Parameters.Add(Db.P("@clubId", clubId));
                    linkCommand.Parameters.Add(Db.P("@joined", dateJoined ?? DateTime.Today));
                    await linkCommand.ExecuteNonQueryAsync();
                    break;
                }

                default:
                    throw new InvalidOperationException($"Unsupported role '{role}'.");
            }

            await transaction.CommitAsync();
            return personId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int?> GetCoordinatorIdAsync(int personId)
    {
        const string sql = "SELECT CoordinatorId FROM dbo.Coordinators WHERE PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? null : (int)result;
    }

    public async Task<int?> GetOrganiserIdAsync(int personId)
    {
        const string sql = "SELECT OrganiserId FROM dbo.Organisers WHERE PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? null : (int)result;
    }

    public async Task<AccountDeleteContext?> GetDeleteContextAsync(int personId)
    {
        const string sql = @"
            SELECT p.PersonId, p.FirstName, p.LastName, p.Username, p.Email,
                   " + RoleCaseSql + @" AS Role,
                   COALESCE(
                       (SELECT TOP 1 c.ClubName FROM dbo.Coordinators x
                        JOIN dbo.FootballClubs c ON c.ClubId = x.ClubId WHERE x.PersonId = p.PersonId),
                       (SELECT TOP 1 c.ClubName FROM dbo.Coaches x
                        JOIN dbo.CoachClubs cc ON cc.CoachId = x.CoachId
                        JOIN dbo.FootballClubs c ON c.ClubId = cc.ClubId
                        WHERE x.PersonId = p.PersonId ORDER BY cc.DateJoined DESC),
                       (SELECT TOP 1 c.ClubName FROM dbo.Players x
                        JOIN dbo.PlayerClubs pc ON pc.PlayerId = x.PlayerId
                        JOIN dbo.FootballClubs c ON c.ClubId = pc.ClubId
                        WHERE x.PersonId = p.PersonId ORDER BY pc.IsActive DESC, pc.DateJoined DESC),
                       (SELECT TOP 1 c.ClubName FROM dbo.Fans x
                        JOIN dbo.FanClubs fc ON fc.FanId = x.FanId
                        JOIN dbo.FootballClubs c ON c.ClubId = fc.ClubId
                        WHERE x.PersonId = p.PersonId ORDER BY fc.DateJoined DESC)
                   ) AS ClubName,
                   (SELECT COUNT(*) FROM dbo.Organisers o
                    JOIN dbo.Competitions c ON c.OrganiserId = o.OrganiserId
                    WHERE o.PersonId = p.PersonId) AS OrganisedCompetitions,
                   (SELECT COUNT(*) FROM dbo.Admins) AS AdminCount
            FROM dbo.Persons p
            " + RoleJoinsSql + @"
            WHERE p.PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new AccountDeleteContext
        {
            PersonId = reader.Int("PersonId"),
            FirstName = reader.Str("FirstName"),
            LastName = reader.Str("LastName"),
            Username = reader.Str("Username"),
            Email = reader.Str("Email"),
            Role = reader.NStr("Role"),
            ClubName = reader.NStr("ClubName"),
            OrganisedCompetitions = reader.Int("OrganisedCompetitions"),
            AdminCount = reader.Int("AdminCount")
        };
    }

    public async Task DeleteAccountAsync(int personId)
    {
        const string playerChildSql = @"
            DELETE FROM dbo.Squads WHERE PlayerId IN (SELECT PlayerId FROM dbo.Players WHERE PersonId = @personId);
            DELETE FROM dbo.PlayerStats WHERE PlayerId IN (SELECT PlayerId FROM dbo.Players WHERE PersonId = @personId);
            DELETE FROM dbo.InjuryHistories WHERE PlayerId IN (SELECT PlayerId FROM dbo.Players WHERE PersonId = @personId);
            DELETE FROM dbo.Unavailabilities WHERE PlayerId IN (SELECT PlayerId FROM dbo.Players WHERE PersonId = @personId);
            DELETE FROM dbo.PlayerClubs WHERE PlayerId IN (SELECT PlayerId FROM dbo.Players WHERE PersonId = @personId);
            DELETE FROM dbo.Players WHERE PersonId = @personId;

            DELETE FROM dbo.CoachClubs WHERE CoachId IN (SELECT CoachId FROM dbo.Coaches WHERE PersonId = @personId);
            DELETE FROM dbo.Coaches WHERE PersonId = @personId;

            DELETE FROM dbo.TeamHistories WHERE CoordinatorId IN (SELECT CoordinatorId FROM dbo.Coordinators WHERE PersonId = @personId);
            DELETE FROM dbo.Announcements WHERE CoordinatorId IN (SELECT CoordinatorId FROM dbo.Coordinators WHERE PersonId = @personId);
            DELETE FROM dbo.Coordinators WHERE PersonId = @personId;

            DELETE FROM dbo.FanClubs WHERE FanId IN (SELECT FanId FROM dbo.Fans WHERE PersonId = @personId);
            DELETE FROM dbo.Fans WHERE PersonId = @personId;

            DELETE FROM dbo.Comments WHERE PersonId = @personId;
            DELETE FROM dbo.Admins WHERE PersonId = @personId;
            DELETE FROM dbo.Organisers WHERE PersonId = @personId;
            DELETE FROM dbo.Persons WHERE PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            await using var command = new SqlCommand(playerChildSql, connection, transaction);
            command.Parameters.Add(Db.P("@personId", personId));
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<DashboardCounts> GetCountsAsync()
    {
        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM dbo.Admins) AS Admins,
                (SELECT COUNT(*) FROM dbo.Organisers) AS Organisers,
                (SELECT COUNT(*) FROM dbo.Coordinators) AS Coordinators,
                (SELECT COUNT(*) FROM dbo.Coaches) AS Coaches,
                (SELECT COUNT(*) FROM dbo.Players) AS Players,
                (SELECT COUNT(*) FROM dbo.Fans) AS Fans,
                (SELECT COUNT(*) FROM dbo.FootballClubs) AS Clubs,
                (SELECT COUNT(*) FROM dbo.Competitions) AS Competitions,
                (SELECT COUNT(*) FROM dbo.Matches) AS Matches;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();

        return new DashboardCounts
        {
            Admins = reader.Int("Admins"),
            Organisers = reader.Int("Organisers"),
            Coordinators = reader.Int("Coordinators"),
            Coaches = reader.Int("Coaches"),
            Players = reader.Int("Players"),
            Fans = reader.Int("Fans"),
            Clubs = reader.Int("Clubs"),
            Competitions = reader.Int("Competitions"),
            Matches = reader.Int("Matches")
        };
    }
}
