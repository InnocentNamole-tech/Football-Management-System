namespace FootballClubManagement.Models;

public sealed class InjuryRow
{
    public int InjuryHistoryId { get; init; }
    public int PlayerId { get; init; }
    public string PlayerName { get; init; } = "";
    public int JerseyNumber { get; init; }
    public int ClubId { get; init; }
    public string InjuryType { get; init; } = "";
    public DateTime InjuryDate { get; init; }
    public string EstimatedRecovery { get; init; } = "";
    public string? ActualRecovery { get; init; }
}

public sealed class UnavailabilityRow
{
    public int UnavailabilityId { get; init; }
    public int PlayerId { get; init; }
    public string PlayerName { get; init; } = "";
    public int JerseyNumber { get; init; }
    public int ClubId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Reason { get; init; } = "";
}

public sealed class AnnouncementRow
{
    public int AnnouncementId { get; init; }
    public int ClubId { get; init; }
    public string ClubName { get; init; } = "";
    public int CoordinatorId { get; init; }
    public string AuthorName { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public DateTime DatePosted { get; init; }
}

public sealed class CommentRow
{
    public int CommentId { get; init; }
    public int MatchId { get; init; }
    public int ClubId { get; init; }
    public string ClubName { get; init; } = "";
    public string Opponent { get; init; } = "";
    public DateTime MatchDate { get; init; }
    public int PersonId { get; init; }
    public string AuthorName { get; init; } = "";
    public string CommentText { get; init; } = "";
    public DateTime PostedAt { get; init; }
}
