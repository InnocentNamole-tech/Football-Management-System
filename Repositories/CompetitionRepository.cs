using FootballClubManagement.Data;
using FootballClubManagement.Models;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Repositories;

public sealed class CompetitionRepository
{
    private readonly Db _db;

    public CompetitionRepository(Db db) => _db = db;

    private const string CompetitionColumnsSql = @"
        SELECT co.CompetitionId, co.CompetitionName, co.CompetitionType, co.Gender, co.AgeGroup,
               co.OrganiserId, op.FirstName + ' ' + op.LastName AS OrganiserName,
               co.StadiumId, st.StadiumName, ci.CityName,
               co.StartDate, co.StartTime, co.EndDate, co.TeamCount,
               co.AffiliationFee, co.RewardPrice,
               co.IndividualAward, co.TrophyMedal, co.Promotion, co.KitSet, co.CompetitionStatus,
               (SELECT COUNT(*) FROM dbo.CompetitionClubs x
                WHERE x.CompetitionId = co.CompetitionId AND x.RequestStatus = 'Approved') AS ApprovedCount,
               (SELECT COUNT(*) FROM dbo.CompetitionClubs x
                WHERE x.CompetitionId = co.CompetitionId AND x.RequestStatus = 'Pending') AS PendingCount";

    private const string CompetitionFromSql = @"
        FROM dbo.Competitions co
        JOIN dbo.Organisers o ON o.OrganiserId = co.OrganiserId
        JOIN dbo.Persons op ON op.PersonId = o.PersonId
        JOIN dbo.Stadiums st ON st.StadiumId = co.StadiumId
        JOIN dbo.Cities ci ON ci.CityId = st.CityId";

    private const string EntryColumnsSql = @",
               cc.CompetitionClubId AS EntryId, cc.RequestStatus, cc.RequestedAt, cc.Outcome, cc.StageReached";

    private const string EntryJoinSql = @"
        LEFT JOIN dbo.CompetitionClubs cc ON cc.CompetitionId = co.CompetitionId AND cc.ClubId = @clubId";

    public async Task<List<CompetitionRow>> ListForOrganiserAsync(int organiserId)
    {
        var sql = CompetitionColumnsSql + CompetitionFromSql + @"
            WHERE co.OrganiserId = @organiserId
            ORDER BY CASE co.CompetitionStatus WHEN 'In Progress' THEN 0 WHEN 'Upcoming' THEN 1 ELSE 2 END,
                     co.StartDate;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@organiserId", organiserId));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<CompetitionRow>();
        while (await reader.ReadAsync()) rows.Add(Map(reader));
        return rows;
    }

    public async Task<CompetitionRow?> GetAsync(int competitionId)
    {
        var sql = CompetitionColumnsSql + CompetitionFromSql + @" WHERE co.CompetitionId = @competitionId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@competitionId", competitionId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<CompetitionRow?> GetForClubAsync(int competitionId, int clubId)
    {
        var sql = CompetitionColumnsSql + EntryColumnsSql + CompetitionFromSql + EntryJoinSql + @"
            WHERE co.CompetitionId = @competitionId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@competitionId", competitionId));
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader, includeEntry: true) : null;
    }

    public async Task<List<CompetitionRow>> ListMatchingForClubAsync(int clubId, string gender, string ageGroup)
    {
        var sql = CompetitionColumnsSql + EntryColumnsSql + CompetitionFromSql + EntryJoinSql + @"
            WHERE co.AgeGroup = @ageGroup AND (co.Gender = @gender OR co.Gender = 'Mixed')
            ORDER BY co.StartDate;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));
        command.Parameters.Add(Db.P("@gender", gender));
        command.Parameters.Add(Db.P("@ageGroup", ageGroup));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<CompetitionRow>();
        while (await reader.ReadAsync()) rows.Add(Map(reader, includeEntry: true));
        return rows;
    }

    public async Task<List<CompetitionEntryRow>> ListEntriesAsync(int clubId, string? requestStatus = null)
    {
        const string sql = @"
            SELECT * FROM dbo.CompetitionEntries
            WHERE ClubId = @clubId AND (@status IS NULL OR RequestStatus = @status)
            ORDER BY RequestedAt DESC;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));
        command.Parameters.Add(Db.P("@status", requestStatus));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<CompetitionEntryRow>();
        while (await reader.ReadAsync()) rows.Add(MapEntry(reader));
        return rows;
    }

    public async Task<List<CompetitionEntryRow>> ListEntriesForOrganiserAsync(int organiserId, string? requestStatus = null)
    {
        const string sql = @"
            SELECT * FROM dbo.CompetitionEntries
            WHERE OrganiserId = @organiserId AND (@status IS NULL OR RequestStatus = @status)
            ORDER BY CASE RequestStatus WHEN 'Pending' THEN 0 WHEN 'Approved' THEN 1 ELSE 2 END,
                     RequestedAt;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@organiserId", organiserId));
        command.Parameters.Add(Db.P("@status", requestStatus));

        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<CompetitionEntryRow>();
        while (await reader.ReadAsync()) rows.Add(MapEntry(reader));
        return rows;
    }

    public async Task<CompetitionEntryRow?> GetEntryForOrganiserAsync(int entryId, int organiserId)
    {
        const string sql = @"
            SELECT * FROM dbo.CompetitionEntries
            WHERE CompetitionClubId = @entryId AND OrganiserId = @organiserId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@entryId", entryId));
        command.Parameters.Add(Db.P("@organiserId", organiserId));

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapEntry(reader) : null;
    }

    public async Task<int> CreateAsync(int organiserId, CompetitionInput input)
    {
        const string sql = @"
            INSERT INTO dbo.Competitions
                (CompetitionName, CompetitionType, Gender, AgeGroup, OrganiserId, StadiumId,
                 StartDate, StartTime, EndDate, TeamCount, AffiliationFee, RewardPrice,
                 IndividualAward, TrophyMedal, Promotion, KitSet, CompetitionStatus)
            OUTPUT INSERTED.CompetitionId
            VALUES (@name, @type, @gender, @ageGroup, @organiserId, @stadiumId,
                    @startDate, @startTime, @endDate, @teamCount, @fee, @reward,
                    @individual, @trophy, @promotion, @kit, @status);";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        AddInputParameters(command, input);
        command.Parameters.Add(Db.P("@organiserId", organiserId));
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> UpdateAsync(int competitionId, int organiserId, CompetitionInput input)
    {
        const string sql = @"
            UPDATE dbo.Competitions
            SET CompetitionName = @name, CompetitionType = @type, Gender = @gender, AgeGroup = @ageGroup,
                StadiumId = @stadiumId, StartDate = @startDate, StartTime = @startTime, EndDate = @endDate,
                TeamCount = @teamCount, AffiliationFee = @fee, RewardPrice = @reward,
                IndividualAward = @individual, TrophyMedal = @trophy, Promotion = @promotion,
                KitSet = @kit, CompetitionStatus = @status
            WHERE CompetitionId = @competitionId AND OrganiserId = @organiserId;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        AddInputParameters(command, input);
        command.Parameters.Add(Db.P("@competitionId", competitionId));
        command.Parameters.Add(Db.P("@organiserId", organiserId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static void AddInputParameters(SqlCommand command, CompetitionInput input)
    {
        command.Parameters.Add(Db.P("@name", input.CompetitionName.Trim()));
        command.Parameters.Add(Db.P("@type", input.CompetitionType));
        command.Parameters.Add(Db.P("@gender", input.Gender));
        command.Parameters.Add(Db.P("@ageGroup", input.AgeGroup));
        command.Parameters.Add(Db.P("@stadiumId", input.StadiumId));
        command.Parameters.Add(Db.P("@startDate", input.StartDate));
        command.Parameters.Add(Db.P("@startTime", input.StartTime));
        command.Parameters.Add(Db.P("@endDate", input.EndDate));
        command.Parameters.Add(Db.P("@teamCount", input.TeamCount));
        command.Parameters.Add(Db.P("@fee", input.AffiliationFee));
        command.Parameters.Add(Db.P("@reward", input.RewardPrice));
        command.Parameters.Add(Db.P("@individual", input.IndividualAward));
        command.Parameters.Add(Db.P("@trophy", input.TrophyMedal));
        command.Parameters.Add(Db.P("@promotion", input.Promotion));
        command.Parameters.Add(Db.P("@kit", input.KitSet));
        command.Parameters.Add(Db.P("@status", input.CompetitionStatus));
    }

    public async Task<int> RequestToJoinAsync(int competitionId, int clubId)
    {
        const string sql = @"
            INSERT INTO dbo.CompetitionClubs (CompetitionId, ClubId, RequestStatus)
            OUTPUT INSERTED.CompetitionClubId
            VALUES (@competitionId, @clubId, 'Pending');";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@competitionId", competitionId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> UpdateRequestStatusAsync(int entryId, int organiserId, string status)
    {
        const string sql = @"
            UPDATE cc
            SET cc.RequestStatus = @status,
                cc.Outcome = CASE WHEN @status = 'Rejected' THEN NULL ELSE cc.Outcome END,
                cc.StageReached = CASE WHEN @status = 'Rejected' THEN NULL ELSE cc.StageReached END
            FROM dbo.CompetitionClubs cc
            JOIN dbo.Competitions co ON co.CompetitionId = cc.CompetitionId
            WHERE cc.CompetitionClubId = @entryId AND co.OrganiserId = @organiserId
                  AND cc.RequestStatus = 'Pending';";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@status", status));
        command.Parameters.Add(Db.P("@entryId", entryId));
        command.Parameters.Add(Db.P("@organiserId", organiserId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> RecordOutcomeAsync(int entryId, int organiserId, string outcome, string? stageReached)
    {
        const string sql = @"
            UPDATE cc
            SET cc.Outcome = @outcome,
                cc.StageReached = CASE WHEN @outcome IN ('In Progress', 'Lost') THEN @stage ELSE NULL END
            FROM dbo.CompetitionClubs cc
            JOIN dbo.Competitions co ON co.CompetitionId = cc.CompetitionId
            WHERE cc.CompetitionClubId = @entryId AND co.OrganiserId = @organiserId
                  AND cc.RequestStatus = 'Approved';";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@outcome", outcome));
        command.Parameters.Add(Db.P("@stage", string.IsNullOrWhiteSpace(stageReached) ? null : stageReached));
        command.Parameters.Add(Db.P("@entryId", entryId));
        command.Parameters.Add(Db.P("@organiserId", organiserId));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<LookupItem>> GetApprovedOptionsAsync(int clubId)
    {
        const string sql = @"
            SELECT co.CompetitionId AS Id, co.CompetitionName AS Name
            FROM dbo.CompetitionClubs cc
            JOIN dbo.Competitions co ON co.CompetitionId = cc.CompetitionId
            WHERE cc.ClubId = @clubId AND cc.RequestStatus = 'Approved'
            ORDER BY co.StartDate;";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@clubId", clubId));

        await using var reader = await command.ExecuteReaderAsync();
        var items = new List<LookupItem>();
        while (await reader.ReadAsync())
        {
            items.Add(new LookupItem { Id = reader.Int("Id"), Name = reader.Str("Name") });
        }

        return items;
    }

    public async Task<bool> IsApprovedAsync(int competitionId, int clubId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM dbo.CompetitionClubs
            WHERE CompetitionId = @competitionId AND ClubId = @clubId AND RequestStatus = 'Approved';";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@competitionId", competitionId));
        command.Parameters.Add(Db.P("@clubId", clubId));
        return (int)(await command.ExecuteScalarAsync() ?? 0) > 0;
    }

    public async Task<int> CountPendingForOrganiserAsync(int organiserId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM dbo.CompetitionClubs cc
            JOIN dbo.Competitions co ON co.CompetitionId = cc.CompetitionId
            WHERE co.OrganiserId = @organiserId AND cc.RequestStatus = 'Pending';";

        await using var connection = await _db.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(Db.P("@organiserId", organiserId));
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    private static CompetitionRow Map(SqlDataReader reader, bool includeEntry = false) => new()
    {
        CompetitionId = reader.Int("CompetitionId"),
        CompetitionName = reader.Str("CompetitionName"),
        CompetitionType = reader.Str("CompetitionType"),
        Gender = reader.Str("Gender"),
        AgeGroup = reader.Str("AgeGroup"),
        OrganiserId = reader.Int("OrganiserId"),
        OrganiserName = reader.Str("OrganiserName"),
        StadiumId = reader.Int("StadiumId"),
        StadiumName = reader.Str("StadiumName"),
        CityName = reader.Str("CityName"),
        StartDate = reader.Date("StartDate"),
        StartTime = reader.TimeOfDay("StartTime"),
        EndDate = reader.Date("EndDate"),
        TeamCount = reader.Int("TeamCount"),
        AffiliationFee = reader.Dec("AffiliationFee"),
        RewardPrice = reader.Int("RewardPrice"),
        IndividualAward = reader.Str("IndividualAward"),
        TrophyMedal = reader.Str("TrophyMedal"),
        Promotion = reader.Str("Promotion"),
        KitSet = reader.Str("KitSet"),
        CompetitionStatus = reader.Str("CompetitionStatus"),
        ApprovedCount = reader.Int("ApprovedCount"),
        PendingCount = reader.Int("PendingCount"),
        EntryId = includeEntry ? reader.NInt("EntryId") : null,
        RequestStatus = includeEntry ? reader.NStr("RequestStatus") : null,
        RequestedAt = includeEntry ? reader.NDate("RequestedAt") : null,
        Outcome = includeEntry ? reader.NStr("Outcome") : null,
        StageReached = includeEntry ? reader.NStr("StageReached") : null
    };

    private static CompetitionEntryRow MapEntry(SqlDataReader reader) => new()
    {
        CompetitionClubId = reader.Int("CompetitionClubId"),
        CompetitionId = reader.Int("CompetitionId"),
        CompetitionName = reader.Str("CompetitionName"),
        CompetitionType = reader.Str("CompetitionType"),
        Gender = reader.Str("Gender"),
        AgeGroup = reader.Str("AgeGroup"),
        StartDate = reader.Date("StartDate"),
        StartTime = reader.TimeOfDay("StartTime"),
        EndDate = reader.Date("EndDate"),
        TeamCount = reader.Int("TeamCount"),
        AffiliationFee = reader.Dec("AffiliationFee"),
        RewardPrice = reader.Int("RewardPrice"),
        IndividualAward = reader.Str("IndividualAward"),
        TrophyMedal = reader.Str("TrophyMedal"),
        Promotion = reader.Str("Promotion"),
        KitSet = reader.Str("KitSet"),
        CompetitionStatus = reader.Str("CompetitionStatus"),
        ClubId = reader.Int("ClubId"),
        ClubName = reader.Str("ClubName"),
        RequestStatus = reader.Str("RequestStatus"),
        RequestedAt = reader.Date("RequestedAt"),
        Outcome = reader.NStr("Outcome"),
        StageReached = reader.NStr("StageReached"),
        OrganiserId = reader.Int("OrganiserId"),
        OrganiserName = reader.Str("OrganiserName"),
        OrganiserEmail = reader.Str("OrganiserEmail"),
        StadiumId = reader.Int("StadiumId"),
        StadiumName = reader.Str("StadiumName"),
        CityName = reader.Str("CityName")
    };
}
