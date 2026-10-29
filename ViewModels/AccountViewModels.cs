using System.ComponentModel.DataAnnotations;
using FootballClubManagement.Models;
using Microsoft.AspNetCore.Http;

namespace FootballClubManagement.ViewModels;

public sealed class LoginVm
{
    [Required(ErrorMessage = "Username is required.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public sealed class CreateAccountVm : IValidatableObject
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
    [Display(Name = "Phone")]
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

    [Required(ErrorMessage = "Choose a role.")]
    public string Role { get; set; } = "";

    [Display(Name = "Club")]
    public int? ClubId { get; set; }

    [Display(Name = "City")]
    public int? CityId { get; set; }

    [Display(Name = "Position")]
    public int? PositionId { get; set; }

    [Range(1, 99, ErrorMessage = "Jersey number must be between 1 and 99.")]
    [Display(Name = "Jersey number")]
    public int? JerseyNumber { get; set; }

    [Display(Name = "Player style")]
    public int? PlayerStyleId { get; set; }

    [StringLength(30, ErrorMessage = "Nickname must be 30 characters or fewer.")]
    public string? Nickname { get; set; }

    [Display(Name = "Specialization")]
    public int? SpecializationId { get; set; }

    [Display(Name = "Coaching licence")]
    public string? LicenseLevel { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date joined")]
    public DateTime? DateJoined { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role is RoleNames.Coordinator or RoleNames.Fan or RoleNames.Coach or RoleNames.Player && ClubId is null)
        {
            yield return new ValidationResult("Choose a club for this role.", new[] { nameof(ClubId) });
        }

        if (Role == RoleNames.Coach && SpecializationId is null)
        {
            yield return new ValidationResult("Choose a specialization for the coach.", new[] { nameof(SpecializationId) });
        }

        if (Role == RoleNames.Coach && !CoachLicenseLevels.All.Contains(LicenseLevel ?? ""))
        {
            yield return new ValidationResult("Choose the coach's licensing level.", new[] { nameof(LicenseLevel) });
        }

        if (Role == RoleNames.Player)
        {
            if (CityId is null) yield return new ValidationResult("Choose the player's city.", new[] { nameof(CityId) });
            if (PositionId is null) yield return new ValidationResult("Choose the player's position.", new[] { nameof(PositionId) });
            if (JerseyNumber is null) yield return new ValidationResult("Enter the player's jersey number.", new[] { nameof(JerseyNumber) });
            if (string.IsNullOrWhiteSpace(Nickname)) yield return new ValidationResult("Enter the player's nickname.", new[] { nameof(Nickname) });
        }
    }
}

public sealed class ResetPasswordVm
{
    public int PersonId { get; set; }
    public string PersonName { get; set; } = "";

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "Use 8 to 64 characters.")]
    [RegularExpression(ValidationPatterns.Password,
        ErrorMessage = "Use at least one uppercase, one lowercase, one digit and one special character.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Confirm the new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class ForgotPasswordVm
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Username is required.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = "";
}

public sealed class SelfResetPasswordVm
{
    public string PersonName { get; set; } = "";

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "Use 8 to 64 characters.")]
    [RegularExpression(ValidationPatterns.Password,
        ErrorMessage = "Use at least one uppercase, one lowercase, one digit and one special character.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Confirm the new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class RegisterVm
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
    [Display(Name = "Phone")]
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

    [Display(Name = "Club")]
    public int? ClubId { get; set; }
}

public sealed class ProfileEditVm : IValidatableObject
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

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(ValidationPatterns.Phone, ErrorMessage = "Enter 10 digits starting with 0.")]
    public string Phone { get; set; } = "";

    [Display(Name = "Profile photo")]
    public IFormFile? Photo { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string? CurrentPassword { get; set; }

    [StringLength(64, MinimumLength = 8, ErrorMessage = "Use 8 to 64 characters.")]
    [RegularExpression(ValidationPatterns.Password,
        ErrorMessage = "Use at least one uppercase, one lowercase, one digit and one special character.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string? NewPassword { get; set; }

    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string? ConfirmPassword { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var started = !string.IsNullOrEmpty(CurrentPassword)
            || !string.IsNullOrEmpty(NewPassword)
            || !string.IsNullOrEmpty(ConfirmPassword);

        if (!started) yield break;

        if (string.IsNullOrEmpty(CurrentPassword))
        {
            yield return new ValidationResult("Enter your current password.", new[] { nameof(CurrentPassword) });
        }

        if (string.IsNullOrEmpty(NewPassword))
        {
            yield return new ValidationResult("Enter a new password.", new[] { nameof(NewPassword) });
        }

        if (string.IsNullOrEmpty(ConfirmPassword))
        {
            yield return new ValidationResult("Confirm the new password.", new[] { nameof(ConfirmPassword) });
        }
    }
}

public sealed class ProfileVm
{
    public Person Person { get; init; } = new();
    public string? Role { get; init; }
    public string? ClubName { get; init; }
    public PlayerOverview? Player { get; init; }
    public CoachRow? Coach { get; init; }
}
