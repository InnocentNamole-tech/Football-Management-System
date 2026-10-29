using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class CoachRepository
{
    private readonly Db _db;

    public CoachRepository(Db db) => _db = db;

    public async Task<List<CoachRow>> ListByClubAsync(int clubId)
    {
        const string sql = @"
            SELECT ch.CoachId, ch.PersonId, pe.FirstName, pe.LastName, pe.Email, pe.Phone,
                   cc.ClubId, cl.ClubName, cc.SpecializationId, sp.SpecializationName,
                   ch.LicenseLevel, cc.DateJoined, pe.ProfilePhotoPath
            FROM dbo.Coaches ch
            JOIN dbo.Persons pe ON pe.PersonId = ch.PersonId
            JOIN dbo.CoachClubs cc ON cc.CoachId = ch.CoachId
            JOIN dbo.FootballClubs cl ON cl.ClubId = cc.ClubId
            JOIN dbo.Specializations sp ON sp.SpecializationId = cc.SpecializationId
            WHERE cc.ClubId = @clubId
            ORDER BY pe.LastName, pe.FirstName;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var coaches = new List<CoachRow>();
        while (await reader.ReadAsync())
        {
            coaches.Add(new CoachRow
            {
                CoachId = reader.Int("CoachId"),
                PersonId = reader.Int("PersonId"),
                FirstName = reader.Str("FirstName"),
                LastName = reader.Str("LastName"),
                Email = reader.Str("Email"),
                Phone = reader.Str("Phone"),
                ClubId = reader.Int("ClubId"),
                ClubName = reader.Str("ClubName"),
                SpecializationId = reader.Int("SpecializationId"),
                SpecializationName = reader.Str("SpecializationName"),
                LicenseLevel = reader.Str("LicenseLevel"),
                DateJoined = reader.Date("DateJoined"),
                ProfilePhotoPath = reader.NStr("ProfilePhotoPath")
            });
        }

        return coaches;
    }

    public async Task<CoachRow?> GetByPersonAsync(int personId)
    {
        const string sql = @"
            SELECT TOP 1 ch.CoachId, ch.PersonId, pe.FirstName, pe.LastName, pe.Email, pe.Phone,
                   cc.ClubId, cl.ClubName, cc.SpecializationId, sp.SpecializationName,
                   ch.LicenseLevel, cc.DateJoined, pe.ProfilePhotoPath
            FROM dbo.Coaches ch
            JOIN dbo.Persons pe ON pe.PersonId = ch.PersonId
            JOIN dbo.CoachClubs cc ON cc.CoachId = ch.CoachId
            JOIN dbo.FootballClubs cl ON cl.ClubId = cc.ClubId
            JOIN dbo.Specializations sp ON sp.SpecializationId = cc.SpecializationId
            WHERE ch.PersonId = @personId
            ORDER BY cc.DateJoined DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new CoachRow
        {
            CoachId = reader.Int("CoachId"),
            PersonId = reader.Int("PersonId"),
            FirstName = reader.Str("FirstName"),
            LastName = reader.Str("LastName"),
            Email = reader.Str("Email"),
            Phone = reader.Str("Phone"),
            ClubId = reader.Int("ClubId"),
            ClubName = reader.Str("ClubName"),
            SpecializationId = reader.Int("SpecializationId"),
            SpecializationName = reader.Str("SpecializationName"),
            LicenseLevel = reader.Str("LicenseLevel"),
            DateJoined = reader.Date("DateJoined"),
            ProfilePhotoPath = reader.NStr("ProfilePhotoPath")
        };
    }

    public async Task<int> CreateAsync(
        string firstName, string lastName, string email, string username, string phone, string passwordHash,
        int clubId, int specializationId, string licenseLevel, DateTime dateJoined)
    {
        const string personSql = @"
            INSERT INTO dbo.Persons (FirstName, LastName, Email, Username, Phone, PasswordHash)
            OUTPUT INSERTED.PersonId
            VALUES (@firstName, @lastName, @email, @username, @phone, @hash);";

        const string coachSql = @"
            INSERT INTO dbo.Coaches (PersonId, LicenseLevel) OUTPUT INSERTED.CoachId VALUES (@personId, @licenseLevel);";

        const string clubSql = @"
            INSERT INTO dbo.CoachClubs (CoachId, ClubId, SpecializationId, DateJoined)
            VALUES (@coachId, @clubId, @specializationId, @joined);";

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

            int coachId;
            await using (var command = new SqlCommand(coachSql, connection, transaction))
            {
                command.Parameters.Add(Db.P("@personId", personId));
                command.Parameters.Add(Db.P("@licenseLevel", licenseLevel));
                coachId = (int)(await command.ExecuteScalarAsync() ?? 0);
            }

            await using (var command = new SqlCommand(clubSql, connection, transaction))
            {
                command.Parameters.Add(Db.P("@coachId", coachId));
                command.Parameters.Add(Db.P("@clubId", clubId));
                command.Parameters.Add(Db.P("@specializationId", specializationId));
                command.Parameters.Add(Db.P("@joined", dateJoined));
                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return coachId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
