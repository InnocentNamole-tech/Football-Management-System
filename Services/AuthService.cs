using System.Security.Claims;
using FootballClubManagement.Models;
using FootballClubManagement.Repositories;

namespace FootballClubManagement.Services;

public static class AppClaims
{
    public const string ClubId = "ClubId";
    public const string FullName = "FullName";
}

public sealed class LoginResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public AuthUser? User { get; init; }
    public ClaimsPrincipal? Principal { get; init; }
}

public sealed class AuthService
{
    private readonly PersonRepository _persons;

    public AuthService(PersonRepository persons) => _persons = persons;

    public Task<AuthUser?> FindForPasswordResetAsync(string email, string username) =>
        _persons.FindForPasswordResetAsync(email, username);

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        var user = await _persons.FindForLoginAsync(username);

        if (user is null)
        {
            return new LoginResult { Success = false, Error = "Invalid username or password." };
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            return new LoginResult
            {
                Success = false,
                Error = $"This account is locked until {user.LockoutEnd.Value.ToLocalTime():HH:mm}. Ask an administrator to unlock it."
            };
        }

        if (user.LockoutEnd.HasValue)
        {
            await _persons.ResetLoginStateAsync(user.PersonId);
            user = new AuthUser
            {
                PersonId = user.PersonId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Username = user.Username,
                PasswordHash = user.PasswordHash,
                FailedLoginCount = 0,
                LockoutEnd = null,
                Role = user.Role,
                ClubId = user.ClubId
            };
        }

        var verification = Passwords.Verify(user.PasswordHash, password);

        if (verification == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
        {
            await _persons.RegisterFailedLoginAsync(user.PersonId);

            if (user.FailedLoginCount + 1 >= 5)
            {
                return new LoginResult
                {
                    Success = false,
                    Error = "Too many failed attempts. This account is locked for 5 minutes."
                };
            }

            return new LoginResult { Success = false, Error = "Invalid username or password." };
        }

        if (string.IsNullOrWhiteSpace(user.Role))
        {
            return new LoginResult { Success = false, Error = "This account has no role assigned. Contact an administrator." };
        }

        if (verification == Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded)
        {
            await _persons.UpdatePasswordAsync(user.PersonId, Passwords.Hash(password));
        }
        else
        {
            await _persons.ResetLoginStateAsync(user.PersonId);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.PersonId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Role, user.Role),
            new(AppClaims.FullName, $"{user.FirstName} {user.LastName}")
        };

        if (user.ClubId.HasValue)
        {
            claims.Add(new Claim(AppClaims.ClubId, user.ClubId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, "FootballClubCookie");
        return new LoginResult
        {
            Success = true,
            User = user,
            Principal = new ClaimsPrincipal(identity)
        };
    }
}
