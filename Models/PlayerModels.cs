namespace FootballClubManagement.Models;

public sealed class PlayerOverview
{
    public int PlayerId { get; init; }
    public int PersonId { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Username { get; init; } = "";
    public string Phone { get; init; } = "";
    public string? ProfilePhotoPath { get; init; }
    public string Nickname { get; init; } = "";
    public int PlayerClubId { get; init; }
    public int ClubId { get; init; }
    public string ClubName { get; init; } = "";
    public int PositionId { get; init; }
    public string PositionName { get; init; } = "";
    public int JerseyNumber { get; init; }
    public bool IsActive { get; init; }
    public DateTime DateJoined { get; init; }
    public int? PlayerStyleId { get; init; }
    public string? PlayerStyleName { get; init; }
    public int CityId { get; init; }
    public string CityName { get; init; } = "";
    public string ProvinceName { get; init; } = "";

    public string FullName => $"{FirstName} {LastName}";

    public string Initials =>
        $"{(FirstName.Length > 0 ? FirstName[0] : ' ')}{(LastName.Length > 0 ? LastName[0] : ' ')}".Trim().ToUpperInvariant();
}

public sealed class PlayerTotalsRow
{
    public int PlayerId { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Nickname { get; init; } = "";
    public int JerseyNumber { get; init; }
    public string PositionName { get; init; } = "";
    public bool IsActive { get; init; }
    public string? ProfilePhotoPath { get; init; }
    public int MatchesPlayed { get; init; }
    public int Goals { get; init; }
    public int Assists { get; init; }
    public int YellowCards { get; init; }
    public int RedCards { get; init; }
    public int MinutesPlayed { get; init; }

    public string FullName => $"{FirstName} {LastName}";
}

public sealed class LeaderRow
{
    public int PlayerId { get; init; }
    public string FullName { get; init; } = "";
    public int JerseyNumber { get; init; }
    public string PositionName { get; init; } = "";
    public int Value { get; init; }
}
