using System.ComponentModel.DataAnnotations;
using FootballClubManagement.Models;

namespace FootballClubManagement.ViewModels;

public sealed class SquadOption
{
    public int PlayerId { get; init; }
    public string FullName { get; init; } = "";
    public int JerseyNumber { get; init; }
    public string PositionName { get; init; } = "";
}

public sealed class SquadStep1Vm
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose a match.")]
    [Display(Name = "Match")]
    public int MatchId { get; set; }

    [Required(ErrorMessage = "Choose a formation.")]
    public string Formation { get; set; } = "";
}

public sealed class SquadSelectionVm
{
    public int MatchId { get; set; }
    public string Formation { get; set; } = "";
    public string MatchLabel { get; set; } = "";
    public string StepTitle { get; set; } = "";
    public string HelpText { get; set; } = "";
    public List<SquadOption> Options { get; set; } = new();
    public List<int> Selected { get; set; } = new();
}

public sealed class SquadReviewVm
{
    public int MatchId { get; set; }
    public string MatchLabel { get; set; } = "";
    public string Formation { get; set; } = "";
    public List<SquadOption> Starters { get; set; } = new();
    public List<SquadOption> Bench { get; set; } = new();
}

public sealed class SquadDraft
{
    public int MatchId { get; set; }
    public string Formation { get; set; } = "";
    public List<int> Starters { get; set; } = new();
    public List<int> Bench { get; set; } = new();
}
