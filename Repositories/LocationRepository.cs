using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class LocationRepository
{
    private readonly Db _db;

    public LocationRepository(Db db) => _db = db;

    public async Task<List<CityRow>> GetCitiesAsync()
    {
        const string sql = @"
            SELECT ci.CityId, ci.CityName, ci.ProvinceId, pr.ProvinceName,
                   (SELECT COUNT(*) FROM dbo.Stadiums st WHERE st.CityId = ci.CityId) AS StadiumCount,
                   (SELECT COUNT(*) FROM dbo.FootballClubs fc WHERE fc.CityId = ci.CityId) AS ClubCount
            FROM dbo.Cities ci
            JOIN dbo.Provinces pr ON pr.ProvinceId = ci.ProvinceId
            ORDER BY pr.ProvinceName, ci.CityName;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var rows = new List<CityRow>();
        while (await reader.ReadAsync())
        {
            rows.Add(new CityRow
            {
                CityId = reader.Int("CityId"),
                CityName = reader.Str("CityName"),
                ProvinceId = reader.Int("ProvinceId"),
                ProvinceName = reader.Str("ProvinceName"),
                StadiumCount = reader.Int("StadiumCount"),
                ClubCount = reader.Int("ClubCount")
            });
        }

        return rows;
    }

    public async Task<CityRow?> GetCityAsync(int cityId)
    {
        const string sql = @"
            SELECT ci.CityId, ci.CityName, ci.ProvinceId, pr.ProvinceName,
                   (SELECT COUNT(*) FROM dbo.Stadiums st WHERE st.CityId = ci.CityId) AS StadiumCount,
                   (SELECT COUNT(*) FROM dbo.FootballClubs fc WHERE fc.CityId = ci.CityId) AS ClubCount
            FROM dbo.Cities ci
            JOIN dbo.Provinces pr ON pr.ProvinceId = ci.ProvinceId
            WHERE ci.CityId = @cityId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@cityId", cityId));

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new CityRow
        {
            CityId = reader.Int("CityId"),
            CityName = reader.Str("CityName"),
            ProvinceId = reader.Int("ProvinceId"),
            ProvinceName = reader.Str("ProvinceName"),
            StadiumCount = reader.Int("StadiumCount"),
            ClubCount = reader.Int("ClubCount")
        };
    }

    public async Task<int> CreateCityAsync(string cityName, int provinceId)
    {
        const string sql = @"
            INSERT INTO dbo.Cities (CityName, ProvinceId)
            OUTPUT INSERTED.CityId
            VALUES (@cityName, @provinceId);";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@cityName", cityName.Trim()));
        command.Parameters.Add(Db.P("@provinceId", provinceId));
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> UpdateCityAsync(int cityId, string cityName, int provinceId)
    {
        const string sql = @"
            UPDATE dbo.Cities
            SET CityName = @cityName, ProvinceId = @provinceId
            WHERE CityId = @cityId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@cityName", cityName.Trim()));
        command.Parameters.Add(Db.P("@provinceId", provinceId));
        command.Parameters.Add(Db.P("@cityId", cityId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<StadiumRow>> GetStadiumsAsync()
    {
        const string sql = @"
            SELECT st.StadiumId, st.StadiumName, st.Capacity, st.Surface,
                   st.CityId, ci.CityName, pr.ProvinceName
            FROM dbo.Stadiums st
            JOIN dbo.Cities ci ON ci.CityId = st.CityId
            JOIN dbo.Provinces pr ON pr.ProvinceId = ci.ProvinceId
            ORDER BY st.StadiumName;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var rows = new List<StadiumRow>();
        while (await reader.ReadAsync()) rows.Add(MapStadium(reader));
        return rows;
    }

    public async Task<StadiumRow?> GetStadiumAsync(int stadiumId)
    {
        const string sql = @"
            SELECT st.StadiumId, st.StadiumName, st.Capacity, st.Surface,
                   st.CityId, ci.CityName, pr.ProvinceName
            FROM dbo.Stadiums st
            JOIN dbo.Cities ci ON ci.CityId = st.CityId
            JOIN dbo.Provinces pr ON pr.ProvinceId = ci.ProvinceId
            WHERE st.StadiumId = @stadiumId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@stadiumId", stadiumId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapStadium(reader) : null;
    }

    public async Task<int> CreateStadiumAsync(string stadiumName, int capacity, string surface, int cityId)
    {
        const string sql = @"
            INSERT INTO dbo.Stadiums (StadiumName, Capacity, Surface, CityId)
            OUTPUT INSERTED.StadiumId
            VALUES (@stadiumName, @capacity, @surface, @cityId);";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@stadiumName", stadiumName.Trim()));
        command.Parameters.Add(Db.P("@capacity", capacity));
        command.Parameters.Add(Db.P("@surface", surface));
        command.Parameters.Add(Db.P("@cityId", cityId));
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> UpdateStadiumAsync(int stadiumId, string stadiumName, int capacity, string surface, int cityId)
    {
        const string sql = @"
            UPDATE dbo.Stadiums
            SET StadiumName = @stadiumName, Capacity = @capacity, Surface = @surface, CityId = @cityId
            WHERE StadiumId = @stadiumId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@stadiumName", stadiumName.Trim()));
        command.Parameters.Add(Db.P("@capacity", capacity));
        command.Parameters.Add(Db.P("@surface", surface));
        command.Parameters.Add(Db.P("@cityId", cityId));
        command.Parameters.Add(Db.P("@stadiumId", stadiumId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static StadiumRow MapStadium(SqlDataReader reader) => new()
    {
        StadiumId = reader.Int("StadiumId"),
        StadiumName = reader.Str("StadiumName"),
        Capacity = reader.Int("Capacity"),
        Surface = reader.Str("Surface"),
        CityId = reader.Int("CityId"),
        CityName = reader.Str("CityName"),
        ProvinceName = reader.Str("ProvinceName")
    };
}
