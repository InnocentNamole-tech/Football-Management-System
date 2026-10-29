using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;

namespace FootballClubManagement.Services;

public sealed class DashboardService
{
    private readonly MatchRepository _matches;
    private readonly PlayerRepository _players;
    private readonly StatisticsRepository _statistics;
    private readonly AnnouncementRepository _announcements;
    private readonly CompetitionRepository _competitions;
    private readonly PersonRepository _persons;
    private readonly InjuryRepository _injuries;
    private readonly UnavailabilityRepository _unavailabilities;
    private readonly ClubRepository _clubs;

    public DashboardService(MatchRepository matches, PlayerRepository players, StatisticsRepository statistics,
        AnnouncementRepository announcements, CompetitionRepository competitions, PersonRepository persons,
        InjuryRepository injuries, UnavailabilityRepository unavailabilities, ClubRepository clubs)
    {
        _matches = matches;
        _players = players;
        _statistics = statistics;
        _announcements = announcements;
        _competitions = competitions;
        _persons = persons;
        _injuries = injuries;
        _unavailabilities = unavailabilities;
        _clubs = clubs;
    }

    public async Task<DashboardVm> BuildAsync(string role, int personId, int? clubId, string fullName, bool showGettingStarted)
    {
        var vm = new DashboardVm
        {
            FullName = fullName,
            Role = role,
            ShowGettingStarted = showGettingStarted,
            GettingStarted = StepsFor(role)
        };

        switch (role)
        {
            case RoleNames.Admin:
                return new DashboardVm
                {
                    FullName = vm.FullName,
                    Role = vm.Role,
                    ShowGettingStarted = vm.ShowGettingStarted,
                    GettingStarted = vm.GettingStarted,
                    Counts = await _persons.GetCountsAsync()
                };

            case RoleNames.Organiser:
            {
                var organiserId = await _persons.GetOrganiserIdAsync(personId);
                if (organiserId is null) return vm;

                var competitions = await _competitions.ListForOrganiserAsync(organiserId.Value);
                var pending = await _competitions.ListEntriesForOrganiserAsync(organiserId.Value, "Pending");

                return new DashboardVm
                {
                    FullName = vm.FullName,
                    Role = vm.Role,
                    ShowGettingStarted = vm.ShowGettingStarted,
                    GettingStarted = vm.GettingStarted,
                    OrganiserCompetitions = competitions,
                    PendingRequests = pending
                };
            }

            case RoleNames.Player:
            {
                var player = await _players.GetByPersonAsync(personId);
                if (player is null) return vm;

                var stats = await _statistics.ListForPlayerAsync(player.PlayerId);
                var totals = new PlayerTotalsRow
                {
                    PlayerId = player.PlayerId,
                    FirstName = player.FirstName,
                    LastName = player.LastName,
                    Nickname = player.Nickname,
                    JerseyNumber = player.JerseyNumber,
                    PositionName = player.PositionName,
                    IsActive = player.IsActive,
                    ProfilePhotoPath = player.ProfilePhotoPath,
                    MatchesPlayed = stats.Count,
                    Goals = stats.Sum(s => s.Goal),
                    Assists = stats.Sum(s => s.Assist),
                    YellowCards = stats.Sum(s => s.YellowCard),
                    RedCards = stats.Sum(s => s.RedCard),
                    MinutesPlayed = stats.Sum(s => s.MinutesPlayed)
                };

                return new DashboardVm
                {
                    FullName = vm.FullName,
                    Role = vm.Role,
                    ClubName = player.ClubName,
                    ShowGettingStarted = vm.ShowGettingStarted,
                    GettingStarted = vm.GettingStarted,
                    Player = player,
                    PlayerTotals = totals,
                    MyMatchStats = stats.Take(5).ToList(),
                    MyInjuries = await _injuries.ListByPlayerAsync(player.PlayerId),
                    MyUnavailabilities = await _unavailabilities.ListByPlayerAsync(player.PlayerId),
                    NextFixture = clubId.HasValue ? await _matches.GetNextFixtureAsync(clubId.Value) : null
                };
            }

            default:
            {
                if (!clubId.HasValue) return vm;

                var club = await _clubs.GetClubAsync(clubId.Value);
                return new DashboardVm
                {
                    FullName = vm.FullName,
                    Role = vm.Role,
                    ClubName = club?.ClubName,
                    ShowGettingStarted = vm.ShowGettingStarted,
                    GettingStarted = vm.GettingStarted,
                    NextFixture = await _matches.GetNextFixtureAsync(clubId.Value),
                    RecentResults = await _matches.GetRecentResultsAsync(clubId.Value, 5),
                    TopScorers = await _players.GetTopAsync(clubId.Value, "Goals"),
                    TopAssists = await _players.GetTopAsync(clubId.Value, "Assists"),
                    YellowCards = await _players.GetTopAsync(clubId.Value, "Yellow"),
                    RedCards = await _players.GetTopAsync(clubId.Value, "Red"),
                    Announcements = (await _announcements.ListByClubAsync(clubId.Value)).Take(3).ToList()
                };
            }
        }
    }

    private static List<GettingStartedStep> StepsFor(string role) => role switch
    {
        RoleNames.Admin => new List<GettingStartedStep>
        {
            new() { Title = "Create an account", Description = "Add a coordinator, coach, player, fan or another organiser.", ActionText = "Create account", Url = "/Admin/Create" },
            new() { Title = "Review all accounts", Description = "Search everyone and check for locked accounts.", ActionText = "View accounts", Url = "/Admin/Accounts" },
            new() { Title = "See the clubs", Description = "Look at the clubs, cities and home stadiums on the platform.", ActionText = "View clubs", Url = "/Clubs" }
        },
        RoleNames.Organiser => new List<GettingStartedStep>
        {
            new() { Title = "Create a competition", Description = "Set the format, dates, venue and rewards.", ActionText = "Create competition", Url = "/Competitions/Create" },
            new() { Title = "Review requests", Description = "Approve or reject clubs that asked to join.", ActionText = "Manage requests", Url = "/Competitions/Requests" },
            new() { Title = "Record outcomes", Description = "Track progress, stage reached and the eventual winner.", ActionText = "Record outcomes", Url = "/Competitions/Outcomes" }
        },
        RoleNames.Coordinator => new List<GettingStartedStep>
        {
            new() { Title = "Register your players", Description = "Add the squad with positions, jersey numbers and profiles.", ActionText = "Register player", Url = "/Players/Create" },
            new() { Title = "Add a match", Description = "Schedule the next fixture and pick the stadium.", ActionText = "Create match", Url = "/Matches/Create" },
            new() { Title = "Post an announcement", Description = "Keep players and fans up to date with club news.", ActionText = "Post announcement", Url = "/Announcements/Create" },
            new() { Title = "Find a competition", Description = "Request to join competitions that match your club.", ActionText = "Browse competitions", Url = "/Competitions" }
        },
        RoleNames.Coach => new List<GettingStartedStep>
        {
            new() { Title = "Pick a squad", Description = "Choose the starting eleven and bench for a match.", ActionText = "Select squad", Url = "/Squads" },
            new() { Title = "Check injuries", Description = "See who is in rehab or unavailable.", ActionText = "View injuries", Url = "/Injuries" },
            new() { Title = "Check availability", Description = "Review player unavailabilities before selection.", ActionText = "View unavailabilities", Url = "/Unavailabilities/Club" }
        },
        RoleNames.Player => new List<GettingStartedStep>
        {
            new() { Title = "Review your statistics", Description = "See your goals, assists, cards and minutes.", ActionText = "View my statistics", Url = "/Statistics/Mine" },
            new() { Title = "Add unavailability", Description = "Tell your coach when you cannot play.", ActionText = "Add unavailability", Url = "/Unavailabilities/Create" },
            new() { Title = "Check the fixtures", Description = "See the next matches for your club.", ActionText = "View fixtures", Url = "/Matches/Fixtures" },
            new() { Title = "Update your profile", Description = "Add a photo and keep your contact details current.", ActionText = "Edit profile", Url = "/Profile/Edit" }
        },
        _ => new List<GettingStartedStep>
        {
            new() { Title = "Follow the fixtures", Description = "See when your club plays next.", ActionText = "View fixtures", Url = "/Matches/Fixtures" },
            new() { Title = "Read the news", Description = "Catch up on announcements from the coordinator.", ActionText = "View announcements", Url = "/Announcements" },
            new() { Title = "Join the conversation", Description = "Comment on matches and results.", ActionText = "Add a comment", Url = "/Comments" }
        }
    };
}
