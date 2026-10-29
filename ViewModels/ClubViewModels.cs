using System.ComponentModel.DataAnnotations;
using FootballClubManagement.Models;

namespace FootballClubManagement.ViewModels;

public sealed class PlayerCreateVm
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be 2 to 50 characters.")]
    [RegularExpression(ValidationPatterns.Name, ErrorMessage = "Use letters and single spaces only.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be 2 to 50 characters.")]
    [RegularExpression(ValidationPatterns.Name, ErrorMessage = "Use letters and single spaces only.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Username is required.")]
    [RegularExpression(ValidationPatterns.Username, ErrorMessage = "4 to 20 letters, digits or underscores.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(ValidationPatterns.Phone, ErrorMessage = "Enter 10 digits starting with 0.")]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "Use 8 to 64 characters.")]
    [RegularExpression(ValidationPatterns.Password,
        ErrorMessage = "Use at least one uppercase, one lowercase, one digit and one special character.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirm the password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Nickname is required.")]
    [StringLength(30, ErrorMessage = "Nickname must be 30 characters or fewer.")]
    public string Nickname { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Choose a city.")]
    [Display(Name = "City")]
    public int CityId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose a position.")]
    [Display(Name = "Position")]
    public int PositionId { get; set; }

    [Range(1, 99, ErrorMessage = "Jersey number must be between 1 and 99.")]
    [Display(Name = "Jersey number")]
    public int JerseyNumber { get; set; }

    [Display(Name = "Player style")]
    public int? PlayerStyleId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date joined")]
    public DateTime DateJoined { get; set; } = DateTime.Today;
}

public sealed class PlayerEditVm
{
    public int PlayerId { get; set; }
    public int PersonId { get; set; }
    public string Username { get; set; } = "";
    public string ClubName { get; set; } = "";

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be 2 to 50 characters.")]
    [RegularExpression(ValidationPatterns.Name, ErrorMessage = "Use letters and single spaces only.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be 2 to 50 characters.")]
    [RegularExpression(ValidationPatterns.Name, ErrorMessage = "Use letters and single spaces only.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(ValidationPatterns.Phone, ErrorMessage = "Enter 10 digits starting with 0.")]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Nickname is required.")]
    [StringLength(30, ErrorMessage = "Nickname must be 30 characters or fewer.")]
    public string Nickname { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Choose a city.")]
    [Display(Name = "City")]
    public int CityId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose a position.")]
    [Display(Name = "Position")]
    public int PositionId { get; set; }

    [Range(1, 99, ErrorMessage = "Jersey number must be between 1 and 99.")]
    [Display(Name = "Jersey number")]
    public int JerseyNumber { get; set; }

    [Display(Name = "Player style")]
    public int? PlayerStyleId { get; set; }
}

public sealed class CoachCreateVm : IValidatableObject
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be 2 to 50 characters.")]
    [RegularExpression(ValidationPatterns.Name, ErrorMessage = "Use letters and single spaces only.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be 2 to 50 characters.")]
    [RegularExpression(ValidationPatterns.Name, ErrorMessage = "Use letters and single spaces only.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Username is required.")]
    [RegularExpression(ValidationPatterns.Username, ErrorMessage = "4 to 20 letters, digits or underscores.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(ValidationPatterns.Phone, ErrorMessage = "Enter 10 digits starting with 0.")]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "Use 8 to 64 characters.")]
    [RegularExpression(ValidationPatterns.Password,
        ErrorMessage = "Use at least one uppercase, one lowercase, one digit and one special character.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirm the password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Choose a specialization.")]
    [Display(Name = "Specialization")]
    public int SpecializationId { get; set; }

    [Required(ErrorMessage = "Choose the coaching licence level.")]
    [Display(Name = "Coaching licence")]
    public string LicenseLevel { get; set; } = "SAFA C";

    [DataType(DataType.Date)]
    [Display(Name = "Date joined")]
    public DateTime DateJoined { get; set; } = DateTime.Today;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!CoachLicenseLevels.All.Contains(LicenseLevel))
        {
            yield return new ValidationResult("Choose a valid coaching licence.", new[] { nameof(LicenseLevel) });
        }
    }
}

public sealed class MatchCreateVm
{
    [Display(Name = "Competition")]
    public int? CompetitionId { get; set; }

    [Required(ErrorMessage = "Opponent is required.")]
    [StringLength(100, ErrorMessage = "Opponent must be 100 characters or fewer.")]
    public string Opponent { get; set; } = "";

    [DataType(DataType.Date)]
    [Display(Name = "Match date")]
    public DateTime MatchDate { get; set; } = DateTime.Today;

    [DataType(DataType.Time)]
    [Display(Name = "Kickoff time")]
    public TimeOnly KickoffTime { get; set; } = new(15, 0);

    [Range(1, int.MaxValue, ErrorMessage = "Choose a stadium.")]
    public int StadiumId { get; set; }
}

public sealed class MatchResultVm
{
    public int MatchId { get; set; }
    public string Opponent { get; set; } = "";
    public DateTime MatchDate { get; set; }

    [Required(ErrorMessage = "Choose the result.")]
    public string Result { get; set; } = "";

    [Range(0, 99, ErrorMessage = "Goals must be between 0 and 99.")]
    [Display(Name = "Goals for")]
    public int GoalFor { get; set; }

    [Range(0, 99, ErrorMessage = "Goals must be between 0 and 99.")]
    [Display(Name = "Goals against")]
    public int GoalAgainst { get; set; }
}

public sealed class StatEntryVm
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose a match.")]
    public int MatchId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose a player.")]
    public int PlayerId { get; set; }

    [Range(0, 99, ErrorMessage = "Goals must be between 0 and 99.")]
    public int Goal { get; set; }

    [Range(0, 99, ErrorMessage = "Assists must be between 0 and 99.")]
    public int Assist { get; set; }

    [Range(0, 2, ErrorMessage = "Yellow cards must be 0 to 2.")]
    [Display(Name = "Yellow cards")]
    public int YellowCard { get; set; }

    [Range(0, 1, ErrorMessage = "Red cards must be 0 or 1.")]
    [Display(Name = "Red cards")]
    public int RedCard { get; set; }

    [Range(0, 130, ErrorMessage = "Minutes must be between 0 and 130.")]
    [Display(Name = "Minutes played")]
    public int MinutesPlayed { get; set; } = 90;
}

public sealed class TeamHistoryVm
{
    public int? HistoryId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(120, ErrorMessage = "Title must be 120 characters or fewer.")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(1000, ErrorMessage = "Description must be 1000 characters or fewer.")]
    public string Description { get; set; } = "";

    [Range(1850, 2100, ErrorMessage = "Year must be between 1850 and 2100.")]
    [Display(Name = "Year")]
    public int Year { get; set; } = DateTime.Today.Year;
}

public sealed class ClubCreateVm : IValidatableObject
{    [Required(ErrorMessage = "Club name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Club name must be 3 to 100 characters.")]
    [Display(Name = "Club name")]
    public string ClubName { get; set; } = "";

    [Range(1850, 2100, ErrorMessage = "Founded year must be between 1850 and 2100.")]
    [Display(Name = "Founded year")]
    public int FoundedYear { get; set; } = DateTime.Today.Year;

    [Required(ErrorMessage = "Choose the gender group.")]
    [Display(Name = "Gender group")]
    public string Gender { get; set; } = "";

    [Required(ErrorMessage = "Choose the age group.")]
    [Display(Name = "Age group")]
    public string AgeGroup { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Choose the home city.")]
    [Display(Name = "Home city")]
    public int CityId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose the home stadium.")]
    [Display(Name = "Home stadium")]
    public int StadiumId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ClubLookups.Genders.Contains(Gender))
        {
            yield return new ValidationResult("Choose a valid gender group.", new[] { nameof(Gender) });
        }

        if (!ClubLookups.AgeGroups.Contains(AgeGroup))
        {
            yield return new ValidationResult("Choose a valid age group.", new[] { nameof(AgeGroup) });
        }
    }
}

public sealed class CityFormVm
{
    public int CityId { get; set; }

    [Required(ErrorMessage = "City name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "City name must be 2 to 100 characters.")]
    [RegularExpression(ValidationPatterns.Name, ErrorMessage = "Use letters and single spaces only.")]
    [Display(Name = "City name")]
    public string CityName { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Choose the province.")]
    [Display(Name = "Province")]
    public int ProvinceId { get; set; }
}

public sealed class StadiumFormVm : IValidatableObject
{
    public int StadiumId { get; set; }

    [Required(ErrorMessage = "Stadium name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Stadium name must be 2 to 100 characters.")]
    [Display(Name = "Stadium name")]
    public string StadiumName { get; set; } = "";

    [Range(1, 200000, ErrorMessage = "Capacity must be between 1 and 200,000.")]
    public int Capacity { get; set; } = 1000;

    [Required(ErrorMessage = "Choose the playing surface.")]
    public string Surface { get; set; } = "Grass";

    [Range(1, int.MaxValue, ErrorMessage = "Choose the city.")]
    public int CityId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!StadiumSurfaces.All.Contains(Surface))
        {
            yield return new ValidationResult("Choose a valid playing surface.", new[] { nameof(Surface) });
        }
    }
}
