namespace FootballClubManagement.Models;

public sealed class MatchRow
{
    public int MatchId { get; init; }
    public int ClubId { get; init; }
    public string ClubName { get; init; } = "";
    public int? CompetitionId { get; init; }
    public string? CompetitionName { get; init; }
    public string? CompetitionType { get; init; }
    public string Opponent { get; init; } = "";
    public DateTime MatchDate { get; init; }
    public TimeSpan KickoffTime { get; init; }
    public int StadiumId { get; init; }
    public string StadiumName { get; init; } = "";
    public string? Formation { get; init; }
    public string? Result { get; init; }
    public int? GoalFor { get; init; }
    public int? GoalAgainst { get; init; }
    public bool HasSquad { get; init; }
    public int CommentCount { get; init; }

    public string Scoreline => Result is null ? "—" : $"{GoalFor} - {GoalAgainst}";
}

public sealed class StatLineRow
{
    public int PlayerStatId { get; init; }
    public int MatchId { get; init; }
    public int PlayerId { get; init; }
    public string PlayerName { get; init; } = "";
    public int JerseyNumber { get; init; }
    public int Goal { get; init; }
    public int Assist { get; init; }
    public int YellowCard { get; init; }
    public int RedCard { get; init; }
    public int MinutesPlayed { get; init; }
}

public sealed class SquadPlayerRow
{
    public int PlayerId { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public int JerseyNumber { get; init; }
    public string PositionName { get; init; } = "";
    public bool IsStarting { get; init; }

    public string FullName => $"{FirstName} {LastName}";
}

public sealed class SquadSummaryRow
{
    public int MatchId { get; init; }
    public string Opponent { get; init; } = "";
    public DateTime MatchDate { get; init; }
    public string? Formation { get; init; }
    public int Starters { get; init; }
    public int Bench { get; init; }
}

public sealed class PlayerMatchStatRow
{
    public int PlayerStatId { get; init; }
    public int MatchId { get; init; }
    public DateTime MatchDate { get; init; }
    public string Opponent { get; init; } = "";
    public string? Result { get; init; }
    public int Goal { get; init; }
    public int Assist { get; init; }
    public int YellowCard { get; init; }
    public int RedCard { get; init; }
    public int MinutesPlayed { get; init; }
}
