using FootballClubManagement.Data;
using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Services;

public sealed class AccountService
{
    private readonly PersonRepository _persons;
    private readonly ClubRepository _clubs;
    private readonly IWebHostEnvironment _env;

    public AccountService(PersonRepository persons, ClubRepository clubs, IWebHostEnvironment env)
    {
        _persons = persons;
        _clubs = clubs;
        _env = env;
    }

    public Task<List<AccountRow>> GetAccountsAsync(string? search) => _persons.GetAccountsAsync(search);

    public Task<Person?> GetAsync(int personId) => _persons.GetAsync(personId);

    public Task<AccountDeleteContext?> GetDeleteContextAsync(int personId) => _persons.GetDeleteContextAsync(personId);

    public async Task<ServiceResult> CreateAsync(CreateAccountVm vm)
    {
        if (!RoleNames.All.Contains(vm.Role))
        {
            return ServiceResult.Fail("Choose a valid role.", nameof(vm.Role));
        }

        if (await _persons.EmailExistsAsync(vm.Email))
        {
            return ServiceResult.Fail("That email address is already registered.", nameof(vm.Email));
        }

        if (await _persons.UsernameExistsAsync(vm.Username))
        {
            return ServiceResult.Fail("That username is already taken.", nameof(vm.Username));
        }

        var hash = Passwords.Hash(vm.Password);
        var dateJoined = vm.Role == RoleNames.Player ? DateTime.Today : (vm.DateJoined ?? DateTime.Today);

        try
        {
            var personId = await _persons.CreateAccountAsync(
                vm.FirstName, vm.LastName, vm.Email, vm.Username, vm.Phone, hash, vm.Role,
                vm.ClubId, vm.Nickname, vm.CityId, vm.PositionId, vm.JerseyNumber,
                vm.PlayerStyleId, vm.SpecializationId, vm.LicenseLevel, dateJoined);

            return ServiceResult.Success(personId);
        }
        catch (SqlException ex) when (SqlErrors.IsUniqueViolation(ex))
        {
            if (ex.Message.Contains("PlayerClubs", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail("That jersey number is already used by an active player in this club.", nameof(vm.JerseyNumber));
            }

            return ServiceResult.Fail("An account with that email or username already exists.", nameof(vm.Username));
        }
    }

    public async Task<ServiceResult> RegisterAsync(RegisterVm vm, string role)
    {
        if (role is not (RoleNames.Fan or RoleNames.Organiser))
        {
            return ServiceResult.Fail("Only fan and organiser accounts can be created here.", nameof(vm.ClubId));
        }

        if (role == RoleNames.Fan)
        {
            if (vm.ClubId is null || await _clubs.GetClubAsync(vm.ClubId.Value) is null)
            {
                return ServiceResult.Fail("Choose the club you support.", nameof(vm.ClubId));
            }
        }

        if (await _persons.EmailExistsAsync(vm.Email))
        {
            return ServiceResult.Fail("That email address is already registered.", nameof(vm.Email));
        }

        if (await _persons.UsernameExistsAsync(vm.Username))
        {
            return ServiceResult.Fail("That username is already taken.", nameof(vm.Username));
        }

        try
        {
            var personId = await _persons.CreateAccountAsync(
                vm.FirstName, vm.LastName, vm.Email, vm.Username, vm.Phone, Passwords.Hash(vm.Password), role,
                role == RoleNames.Fan ? vm.ClubId : null, null, null, null, null, null, null, null, DateTime.Today);

            return ServiceResult.Success(personId);
        }
        catch (SqlException ex) when (SqlErrors.IsUniqueViolation(ex))
        {
            return ServiceResult.Fail("An account with that email or username already exists.", nameof(vm.Username));
        }
    }

    public async Task<ServiceResult> ResetPasswordAsync(int personId, string newPassword)
    {
        var person = await _persons.GetAsync(personId);
        if (person is null) return ServiceResult.Fail("That account no longer exists.");

        await _persons.UpdatePasswordAsync(personId, Passwords.Hash(newPassword));
        return ServiceResult.Success(personId);
    }

    public async Task<ServiceResult> UnlockAsync(int personId)
    {
        var person = await _persons.GetAsync(personId);
        if (person is null) return ServiceResult.Fail("That account no longer exists.");

        await _persons.UnlockAsync(personId);
        return ServiceResult.Success(personId);
    }

    public async Task<ServiceResult> DeleteAsync(int personId, int currentPersonId)
    {
        if (personId == currentPersonId)
        {
            return ServiceResult.Fail("You cannot remove your own account.");
        }

        var person = await _persons.GetAsync(personId);
        var context = await _persons.GetDeleteContextAsync(personId);
        if (person is null || context is null)
        {
            return ServiceResult.Fail("That account no longer exists.");
        }

        if (context.Role == RoleNames.Admin && context.AdminCount <= 1)
        {
            return ServiceResult.Fail("The last system administrator cannot be removed.");
        }

        if (context.OrganisedCompetitions > 0)
        {
            return ServiceResult.Fail("This organiser still owns competitions. Remove or reassign those competitions first.");
        }

        await _persons.DeleteAccountAsync(personId);
        DeletePhoto(person.ProfilePhotoPath);
        return ServiceResult.Success(personId);
    }

    private void DeletePhoto(string? photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath)) return;

        var fileName = Path.GetFileName(photoPath);
        if (string.IsNullOrWhiteSpace(fileName)) return;

        var path = Path.Combine(_env.WebRootPath, "uploads", fileName);
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
            // The account is already gone; an orphaned file is harmless.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
