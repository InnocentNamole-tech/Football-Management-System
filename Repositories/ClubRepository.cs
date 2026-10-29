using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class ClubRepository
{
    private readonly Db _db;

    public ClubRepository(Db db) => _db = db;

    public async Task<List<ClubRow>> GetClubsAsync()
    {
        const string sql = @"
            SELECT fc.ClubId, fc.ClubName, fc.FoundedYear, fc.Gender, fc.AgeGroup,
                   fc.CityId, ci.CityName, pr.ProvinceName,
                   fc.HomeStadiumId AS StadiumId, st.StadiumName, st.Capacity
            FROM dbo.FootballClubs fc
            JOIN dbo.Cities ci ON ci.CityId = fc.CityId
            JOIN dbo.Provinces pr ON pr.ProvinceId = ci.ProvinceId
            JOIN dbo.Stadiums st ON st.StadiumId = fc.HomeStadiumId
            ORDER BY fc.ClubName;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var clubs = new List<ClubRow>();
        while (await reader.ReadAsync())
        {
            clubs.Add(new ClubRow
            {
                ClubId = reader.Int("ClubId"),
                ClubName = reader.Str("ClubName"),
                FoundedYear = reader.Int("FoundedYear"),
                Gender = reader.Str("Gender"),
                AgeGroup = reader.Str("AgeGroup"),
                CityId = reader.Int("CityId"),
                CityName = reader.Str("CityName"),
                ProvinceName = reader.Str("ProvinceName"),
                StadiumId = reader.Int("StadiumId"),
                StadiumName = reader.Str("StadiumName"),
                Capacity = reader.Int("Capacity")
            });
        }

        return clubs;
    }

    public async Task<ClubRow?> GetClubAsync(int clubId)
    {
        var clubs = await GetClubsAsync();
        return clubs.FirstOrDefault(c => c.ClubId == clubId);
    }

    public async Task<int> CreateClubAsync(string clubName, int foundedYear, string gender, string ageGroup,
        int cityId, int stadiumId)
    {
        const string sql = @"
            INSERT INTO dbo.FootballClubs (ClubName, FoundedYear, Gender, AgeGroup, CityId, HomeStadiumId)
            OUTPUT INSERTED.ClubId
            VALUES (@clubName, @foundedYear, @gender, @ageGroup, @cityId, @stadiumId);";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubName", clubName.Trim()));
        command.Parameters.Add(Db.P("@foundedYear", foundedYear));
        command.Parameters.Add(Db.P("@gender", gender));
        command.Parameters.Add(Db.P("@ageGroup", ageGroup));
        command.Parameters.Add(Db.P("@cityId", cityId));
        command.Parameters.Add(Db.P("@stadiumId", stadiumId));
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<ClubOverview?> GetOverviewAsync(int clubId)
    {
        const string sql = @"
            SELECT fc.ClubId, fc.ClubName, fc.FoundedYear, fc.Gender, fc.AgeGroup,
                   fc.CityId, ci.CityName, pr.ProvinceName,
                   fc.HomeStadiumId AS StadiumId, st.StadiumName, st.Capacity,
                   (SELECT TOP 1 pe.FirstName + ' ' + pe.LastName
                    FROM dbo.CoachClubs cc
                    JOIN dbo.Coaches ch ON ch.CoachId = cc.CoachId
                    JOIN dbo.Persons pe ON pe.PersonId = ch.PersonId
                    WHERE cc.ClubId = fc.ClubId
                    ORDER BY cc.DateJoined DESC) AS CoachName,
                   (SELECT TOP 1 ch.LicenseLevel
                    FROM dbo.CoachClubs cc
                    JOIN dbo.Coaches ch ON ch.CoachId = cc.CoachId
                    WHERE cc.ClubId = fc.ClubId
                    ORDER BY cc.DateJoined DESC) AS CoachLicense,
                   (SELECT TOP 1 pe.FirstName + ' ' + pe.LastName
                    FROM dbo.Coordinators co
                    JOIN dbo.Persons pe ON pe.PersonId = co.PersonId
                    WHERE co.ClubId = fc.ClubId) AS CoordinatorName,
                   (SELECT COUNT(*) FROM dbo.PlayerClubs pc
                    WHERE pc.ClubId = fc.ClubId AND pc.IsActive = 1) AS TotalPlayers,
                   (SELECT COUNT(*) FROM dbo.Matches m
                    WHERE m.ClubId = fc.ClubId AND m.Result IS NOT NULL) AS MatchesPlayed,
                   (SELECT COUNT(*) FROM dbo.Matches m
                    WHERE m.ClubId = fc.ClubId AND m.Result = 'Win') AS Wins
            FROM dbo.FootballClubs fc
            JOIN dbo.Cities ci ON ci.CityId = fc.CityId
            JOIN dbo.Provinces pr ON pr.ProvinceId = ci.ProvinceId
            JOIN dbo.Stadiums st ON st.StadiumId = fc.HomeStadiumId
            WHERE fc.ClubId = @clubId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new ClubOverview
        {
            Club = new ClubRow
            {
                ClubId = reader.Int("ClubId"),
                ClubName = reader.Str("ClubName"),
                FoundedYear = reader.Int("FoundedYear"),
                Gender = reader.Str("Gender"),
                AgeGroup = reader.Str("AgeGroup"),
                CityId = reader.Int("CityId"),
                CityName = reader.Str("CityName"),
                ProvinceName = reader.Str("ProvinceName"),
                StadiumId = reader.Int("StadiumId"),
                StadiumName = reader.Str("StadiumName"),
                Capacity = reader.Int("Capacity")
            },
            CoachName = reader.NStr("CoachName"),
            CoachLicense = reader.NStr("CoachLicense"),
            CoordinatorName = reader.NStr("CoordinatorName"),
            TotalPlayers = reader.Int("TotalPlayers"),
            MatchesPlayed = reader.Int("MatchesPlayed"),
            Wins = reader.Int("Wins")
        };
    }

    public Task<List<LookupItem>> GetProvincesAsync() =>
        GetLookupAsync("SELECT ProvinceId AS Id, ProvinceName AS Name FROM dbo.Provinces ORDER BY ProvinceName;");

    public Task<List<LookupItem>> GetCitiesAsync() =>
        GetLookupAsync(@"SELECT ci.CityId AS Id, ci.CityName + ' (' + pr.ProvinceName + ')' AS Name
                         FROM dbo.Cities ci JOIN dbo.Provinces pr ON pr.ProvinceId = ci.ProvinceId
                         ORDER BY ci.CityName;");

    public Task<List<LookupItem>> GetStadiumsAsync() =>
        GetLookupAsync(@"SELECT st.StadiumId AS Id, st.StadiumName + ' - ' + ci.CityName AS Name
                         FROM dbo.Stadiums st JOIN dbo.Cities ci ON ci.CityId = st.CityId
                         ORDER BY st.StadiumName;");

    public Task<List<LookupItem>> GetPositionsAsync() =>
        GetLookupAsync("SELECT PositionId AS Id, PositionName AS Name FROM dbo.Positions ORDER BY PositionName;");

    public Task<List<LookupItem>> GetSpecializationsAsync() =>
        GetLookupAsync("SELECT SpecializationId AS Id, SpecializationName AS Name FROM dbo.Specializations ORDER BY SpecializationName;");

    public Task<List<LookupItem>> GetPlayerStylesAsync() =>
        GetLookupAsync("SELECT PlayerStyleId AS Id, PlayerStyleName AS Name FROM dbo.PlayerStyles ORDER BY PlayerStyleName;");

    private async Task<List<LookupItem>> GetLookupAsync(string sql)
    {
        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var items = new List<LookupItem>();
        while (await reader.ReadAsync())
        {
            items.Add(new LookupItem { Id = reader.Int("Id"), Name = reader.Str("Name") });
        }

        return items;
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

    public async Task<int?> GetCoachIdAsync(int personId)
    {
        const string sql = "SELECT CoachId FROM dbo.Coaches WHERE PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? null : (int)result;
    }

    public async Task<int?> GetFanIdAsync(int personId)
    {
        const string sql = "SELECT FanId FROM dbo.Fans WHERE PersonId = @personId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@personId", personId));
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? null : (int)result;
    }
}
