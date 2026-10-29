using System.ComponentModel.DataAnnotations;
using FootballClubManagement.Models;

namespace FootballClubManagement.ViewModels;

public sealed class InjuryVm
{
    public int? InjuryHistoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose a player.")]
    [Display(Name = "Player")]
    public int PlayerId { get; set; }

    [Required(ErrorMessage = "Injury type is required.")]
    [StringLength(100, ErrorMessage = "Injury type must be 100 characters or fewer.")]
    [Display(Name = "Injury type")]
    public string InjuryType { get; set; } = "";

    [DataType(DataType.Date)]
    [Display(Name = "Injury date")]
    public DateTime InjuryDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Estimated recovery is required.")]
    [StringLength(50, ErrorMessage = "Estimated recovery must be 50 characters or fewer.")]
    [Display(Name = "Estimated recovery")]
    public string EstimatedRecovery { get; set; } = "";

    [StringLength(50, ErrorMessage = "Actual recovery must be 50 characters or fewer.")]
    [Display(Name = "Actual recovery")]
    public string? ActualRecovery { get; set; }
}

public sealed class UnavailabilityVm : IValidatableObject
{
    public int? UnavailabilityId { get; set; }
    public string PlayerName { get; set; } = "";

    [DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "End date")]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Reason is required.")]
    [StringLength(200, ErrorMessage = "Reason must be 200 characters or fewer.")]
    public string Reason { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult("End date cannot be before the start date.", new[] { nameof(EndDate) });
        }
    }
}

public sealed class AnnouncementVm
{
    public int? AnnouncementId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(120, ErrorMessage = "Title must be 120 characters or fewer.")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(1000, ErrorMessage = "Description must be 1000 characters or fewer.")]
    public string Description { get; set; } = "";
}

public sealed class CommentVm
{
    public int? CommentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose a match.")]
    [Display(Name = "Match")]
    public int MatchId { get; set; }

    [Required(ErrorMessage = "Comment is required.")]
    [StringLength(500, ErrorMessage = "Comment must be 500 characters or fewer.")]
    [Display(Name = "Comment")]
    public string CommentText { get; set; } = "";
}
