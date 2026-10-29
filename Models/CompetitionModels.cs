namespace FootballClubManagement.Models;

public sealed class CompetitionRow
{
    public int CompetitionId { get; init; }
    public string CompetitionName { get; init; } = "";
    public string CompetitionType { get; init; } = "";
    public string Gender { get; init; } = "";
    public string AgeGroup { get; init; } = "";
    public int OrganiserId { get; init; }
    public string OrganiserName { get; init; } = "";
    public int StadiumId { get; init; }
    public string StadiumName { get; init; } = "";
    public string CityName { get; init; } = "";
    public DateTime StartDate { get; init; }
    public TimeSpan StartTime { get; init; }
    public DateTime EndDate { get; init; }
    public int TeamCount { get; init; }
    public decimal AffiliationFee { get; init; }
    public int RewardPrice { get; init; }
    public string IndividualAward { get; init; } = "No";
    public string TrophyMedal { get; init; } = "No";
    public string Promotion { get; init; } = "No";
    public string KitSet { get; init; } = "No";
    public string CompetitionStatus { get; init; } = "Upcoming";
    public int ApprovedCount { get; init; }
    public int PendingCount { get; init; }

    public int? EntryId { get; init; }
    public string? RequestStatus { get; init; }
    public DateTime? RequestedAt { get; init; }
    public string? Outcome { get; init; }
    public string? StageReached { get; init; }
}

public sealed class CompetitionInput
{
    public string CompetitionName { get; set; } = "";
    public string CompetitionType { get; set; } = "League";
    public string Gender { get; set; } = "Men";
    public string AgeGroup { get; set; } = "Senior";
    public int StadiumId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public TimeSpan StartTime { get; set; } = new(15, 0, 0);
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);
    public int TeamCount { get; set; } = 8;
    public decimal AffiliationFee { get; set; }
    public int RewardPrice { get; set; }
    public string IndividualAward { get; set; } = "No";
    public string TrophyMedal { get; set; } = "No";
    public string Promotion { get; set; } = "No";
    public string KitSet { get; set; } = "No";
    public string CompetitionStatus { get; set; } = "Upcoming";
}

public sealed class CompetitionEntryRow
{
    public int CompetitionClubId { get; init; }
    public int CompetitionId { get; init; }
    public string CompetitionName { get; init; } = "";
    public string CompetitionType { get; init; } = "";
    public string Gender { get; init; } = "";
    public string AgeGroup { get; init; } = "";
    public DateTime StartDate { get; init; }
    public TimeSpan StartTime { get; init; }
    public DateTime EndDate { get; init; }
    public int TeamCount { get; init; }
    public decimal AffiliationFee { get; init; }
    public int RewardPrice { get; init; }
    public string IndividualAward { get; init; } = "No";
    public string TrophyMedal { get; init; } = "No";
    public string Promotion { get; init; } = "No";
    public string KitSet { get; init; } = "No";
    public string CompetitionStatus { get; init; } = "Upcoming";
    public int ClubId { get; init; }
    public string ClubName { get; init; } = "";
    public string RequestStatus { get; init; } = "Pending";
    public DateTime RequestedAt { get; init; }
    public string? Outcome { get; init; }
    public string? StageReached { get; init; }
    public int OrganiserId { get; init; }
    public string OrganiserName { get; init; } = "";
    public string OrganiserEmail { get; init; } = "";
    public int StadiumId { get; init; }
    public string StadiumName { get; init; } = "";
    public string CityName { get; init; } = "";
}
