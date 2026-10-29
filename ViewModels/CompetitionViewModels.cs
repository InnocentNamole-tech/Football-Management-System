using System.ComponentModel.DataAnnotations;
using FootballClubManagement.Models;

namespace FootballClubManagement.ViewModels;

public sealed class CompetitionVm : IValidatableObject{
    public int? CompetitionId { get; set; }

    [Required(ErrorMessage = "Competition name is required.")]
    [StringLength(100, ErrorMessage = "Name must be 100 characters or fewer.")]
    [Display(Name = "Competition name")]
    public string CompetitionName { get; set; } = "";

    [Required(ErrorMessage = "Choose a type.")]
    [Display(Name = "Competition type")]
    public string CompetitionType { get; set; } = "League";

    [Required(ErrorMessage = "Choose a gender.")]
    public string Gender { get; set; } = "Men";

    [Required(ErrorMessage = "Choose an age group.")]
    [Display(Name = "Age group")]
    public string AgeGroup { get; set; } = "Senior";

    [Range(1, int.MaxValue, ErrorMessage = "Choose a stadium.")]
    [Display(Name = "Stadium")]
    public int StadiumId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Time)]
    [Display(Name = "Start time")]
    public TimeOnly StartTime { get; set; } = new(15, 0);

    [DataType(DataType.Date)]
    [Display(Name = "End date")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);

    [Range(2, 128, ErrorMessage = "Team count must be between 2 and 128.")]
    [Display(Name = "Team count")]
    public int TeamCount { get; set; } = 8;

    [Range(typeof(decimal), "0", "10000000", ErrorMessage = "Affiliation fee cannot be negative.")]
    [Display(Name = "Affiliation fee")]
    public decimal AffiliationFee { get; set; }

    [Range(0, 100000000, ErrorMessage = "Reward price cannot be negative.")]
    [Display(Name = "Reward price")]
    public int RewardPrice { get; set; }

    [Display(Name = "Individual award")]
    public string IndividualAward { get; set; } = "No";

    [Display(Name = "Trophy / medal")]
    public string TrophyMedal { get; set; } = "No";

    public string Promotion { get; set; } = "No";

    [Display(Name = "Kit set")]
    public string KitSet { get; set; } = "No";

    [Display(Name = "Status")]
    public string CompetitionStatus { get; set; } = "Upcoming";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult("End date cannot be before the start date.", new[] { nameof(EndDate) });
        }
    }
}

public sealed class OutcomeVm
{
    public int CompetitionClubId { get; set; }
    public string CompetitionName { get; set; } = "";
    public string ClubName { get; set; } = "";

    [Required(ErrorMessage = "Choose an outcome.")]
    public string Outcome { get; set; } = "";

    [Display(Name = "Stage reached")]
    public string? StageReached { get; set; }
}

public sealed class CompetitionsIndexVm
{
    public string Role { get; init; } = "";
    public string? ClubName { get; init; }
    public List<CompetitionRow> Competitions { get; init; } = new();
    public List<CompetitionEntryRow> Entries { get; init; } = new();
    public int PendingCount { get; init; }
}
