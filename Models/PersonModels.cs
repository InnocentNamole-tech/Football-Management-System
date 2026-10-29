namespace FootballClubManagement.Models;

public sealed class Person
{
    public int PersonId { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Username { get; init; } = "";
    public string Phone { get; init; } = "";
    public string? ProfilePhotoPath { get; init; }
    public DateTime CreatedAt { get; init; }

    public string FullName => $"{FirstName} {LastName}";
}

public sealed class AuthUser
{
    public int PersonId { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Username { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public int FailedLoginCount { get; init; }
    public DateTime? LockoutEnd { get; init; }
    public string? Role { get; init; }
    public int? ClubId { get; init; }

    public string FullName => $"{FirstName} {LastName}";
}

public sealed class AccountRow
{
    public int PersonId { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Username { get; init; } = "";
    public string? Role { get; init; }
    public string? ClubName { get; init; }
    public int FailedLoginCount { get; init; }
    public DateTime? LockoutEnd { get; init; }
    public DateTime CreatedAt { get; init; }

    public string FullName => $"{FirstName} {LastName}";

    public bool IsLocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
}

public sealed class AccountDeleteContext
{
    public int PersonId { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Role { get; init; }
    public string? ClubName { get; init; }
    public int OrganisedCompetitions { get; init; }
    public int AdminCount { get; init; }

    public string FullName => $"{FirstName} {LastName}";
}

public sealed class DashboardCounts
{
    public int Admins { get; init; }
    public int Organisers { get; init; }
    public int Coordinators { get; init; }
    public int Coaches { get; init; }
    public int Players { get; init; }
    public int Fans { get; init; }
    public int Clubs { get; init; }
    public int Competitions { get; init; }
    public int Matches { get; init; }
}
