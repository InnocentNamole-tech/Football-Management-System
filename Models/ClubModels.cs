namespace FootballClubManagement.Models;

public sealed class ClubRow
{
    public int ClubId { get; init; }
    public string ClubName { get; init; } = "";
    public int FoundedYear { get; init; }
    public string Gender { get; init; } = "";
    public string AgeGroup { get; init; } = "";
    public int CityId { get; init; }
    public string CityName { get; init; } = "";
    public string ProvinceName { get; init; } = "";
    public int StadiumId { get; init; }
    public string StadiumName { get; init; } = "";
    public int Capacity { get; init; }
}

public sealed class CoachRow
{
    public int CoachId { get; init; }
    public int PersonId { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Phone { get; init; } = "";
    public int ClubId { get; init; }
    public string ClubName { get; init; } = "";
    public int SpecializationId { get; init; }
    public string SpecializationName { get; init; } = "";
    public string LicenseLevel { get; init; } = "";
    public DateTime DateJoined { get; init; }
    public string? ProfilePhotoPath { get; init; }

    public string FullName => $"{FirstName} {LastName}";
}

public sealed class ClubOverview
{
    public ClubRow Club { get; init; } = new();
    public string? CoachName { get; init; }
    public string? CoachLicense { get; init; }
    public string? CoordinatorName { get; init; }
    public int TotalPlayers { get; init; }
    public int MatchesPlayed { get; init; }
    public int Wins { get; init; }
}

public sealed class TeamHistoryRow
{
    public int HistoryId { get; init; }
    public int CoordinatorId { get; init; }
    public string AuthorName { get; init; } = "";
    public int ClubId { get; init; }
    public string ClubName { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public int Year { get; init; }
}

public sealed class CityRow
{
    public int CityId { get; init; }
    public string CityName { get; init; } = "";
    public int ProvinceId { get; init; }
    public string ProvinceName { get; init; } = "";
    public int StadiumCount { get; init; }
    public int ClubCount { get; init; }
}

public sealed class StadiumRow
{
    public int StadiumId { get; init; }
    public string StadiumName { get; init; } = "";
    public int Capacity { get; init; }
    public string Surface { get; init; } = "";
    public int CityId { get; init; }
    public string CityName { get; init; } = "";
    public string ProvinceName { get; init; } = "";
}
