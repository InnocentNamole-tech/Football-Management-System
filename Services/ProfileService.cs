using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace FootballClubManagement.Services;

public sealed class ProfileService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png" };
    private const long MaxPhotoBytes = 2 * 1024 * 1024;

    private readonly PersonRepository _persons;
    private readonly PlayerRepository _players;
    private readonly CoachRepository _coaches;
    private readonly IWebHostEnvironment _environment;

    public ProfileService(PersonRepository persons, PlayerRepository players, CoachRepository coaches,
        IWebHostEnvironment environment)
    {
        _persons = persons;
        _players = players;
        _coaches = coaches;
        _environment = environment;
    }

    public async Task<ProfileVm?> GetAsync(int personId)
    {
        var person = await _persons.GetAsync(personId);
        if (person is null) return null;

        var (role, clubName) = await _persons.GetRoleInfoAsync(personId);
        var player = role == RoleNames.Player ? await _players.GetByPersonAsync(personId) : null;
        var coach = role == RoleNames.Coach ? await _coaches.GetByPersonAsync(personId) : null;

        return new ProfileVm
        {
            Person = person,
            Role = role,
            ClubName = clubName,
            Player = player,
            Coach = coach
        };
    }

    public async Task<ServiceResult> UpdateAsync(int personId, ProfileEditVm vm)
    {
        var person = await _persons.GetAsync(personId);
        if (person is null) return ServiceResult.Fail("Your account could not be found.");

        if (await _persons.EmailExistsAsync(vm.Email, personId))
        {
            return ServiceResult.Fail("That email address is already registered.", nameof(vm.Email));
        }

        var changingPassword = !string.IsNullOrEmpty(vm.NewPassword);
        if (changingPassword)
        {
            var currentHash = await _persons.GetPasswordHashAsync(personId);
            if (currentHash is null
                || Passwords.Verify(currentHash, vm.CurrentPassword ?? "") == PasswordVerificationResult.Failed)
            {
                return ServiceResult.Fail("Your current password is incorrect.", nameof(vm.CurrentPassword));
            }
        }

        string? newPhotoPath = null;

        if (vm.Photo is { Length: > 0 })
        {
            if (vm.Photo.Length > MaxPhotoBytes)
            {
                return ServiceResult.Fail("The photo must be 2 MB or smaller.", nameof(vm.Photo));
            }

            var extension = Path.GetExtension(vm.Photo.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(vm.Photo.ContentType.ToLowerInvariant()))
            {
                return ServiceResult.Fail("Only JPG or PNG images are allowed.", nameof(vm.Photo));
            }

            var uploads = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploads);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploads, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
            {
                await vm.Photo.CopyToAsync(stream);
            }

            newPhotoPath = $"/uploads/{fileName}";
        }

        await _persons.UpdateProfileAsync(personId, vm.FirstName, vm.LastName, vm.Email, vm.Phone, newPhotoPath);

        if (newPhotoPath is not null && !string.IsNullOrWhiteSpace(person.ProfilePhotoPath)
            && person.ProfilePhotoPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var oldPath = Path.Combine(_environment.WebRootPath, person.ProfilePhotoPath.TrimStart('/'));
            if (System.IO.File.Exists(oldPath))
            {
                System.IO.File.Delete(oldPath);
            }
        }

        if (changingPassword)
        {
            await _persons.UpdatePasswordAsync(personId, Passwords.Hash(vm.NewPassword!));
        }

        return ServiceResult.Success(personId);
    }
}
