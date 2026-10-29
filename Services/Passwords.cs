using FootballClubManagement.Models;
using Microsoft.AspNetCore.Identity;

namespace FootballClubManagement.Services;

public static class Passwords
{
    private static readonly PasswordHasher<Person> Hasher = new();

    public static string Hash(string password) => Hasher.HashPassword(new Person(), password);

    public static PasswordVerificationResult Verify(string passwordHash, string password) =>
        Hasher.VerifyHashedPassword(new Person(), passwordHash, password);
}
