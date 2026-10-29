using FootballClubManagement.Models;

namespace FootballClubManagement.ViewModels;

public sealed class GettingStartedStep
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string ActionText { get; init; } = "";
    public string Url { get; init; } = "";
}

public sealed class DashboardVm
{
    public string FullName { get; init; } = "";
    public string Role { get; init; } = "";
    public string? ClubName { get; init; }
    public bool ShowGettingStarted { get; init; }
    public List<GettingStartedStep> GettingStarted { get; init; } = new();

    public MatchRow? NextFixture { get; init; }
    public List<MatchRow> RecentResults { get; init; } = new();
    public List<LeaderRow> TopScorers { get; init; } = new();
    public List<LeaderRow> TopAssists { get; init; } = new();
    public List<LeaderRow> YellowCards { get; init; } = new();
    public List<LeaderRow> RedCards { get; init; } = new();
    public List<AnnouncementRow> Announcements { get; init; } = new();

    public PlayerOverview? Player { get; init; }
    public PlayerTotalsRow? PlayerTotals { get; init; }
    public List<PlayerMatchStatRow> MyMatchStats { get; init; } = new();
    public List<InjuryRow> MyInjuries { get; init; } = new();
    public List<UnavailabilityRow> MyUnavailabilities { get; init; } = new();

    public List<CompetitionRow> OrganiserCompetitions { get; init; } = new();
    public List<CompetitionEntryRow> PendingRequests { get; init; } = new();
    public DashboardCounts? Counts { get; init; }
}
