using FootballClubManagement.Data;
using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Services;

public sealed class CoachService
{
    private readonly CoachRepository _coaches;
    private readonly PersonRepository _persons;

    public CoachService(CoachRepository coaches, PersonRepository persons)
    {
        _coaches = coaches;
        _persons = persons;
    }

    public Task<List<CoachRow>> ListAsync(int clubId) => _coaches.ListByClubAsync(clubId);

    public async Task<ServiceResult> CreateAsync(CoachCreateVm vm, int clubId)
    {
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
            var coachId = await _coaches.CreateAsync(
                vm.FirstName, vm.LastName, vm.Email, vm.Username, vm.Phone, Passwords.Hash(vm.Password),
                clubId, vm.SpecializationId, vm.LicenseLevel, vm.DateJoined);

            return ServiceResult.Success(coachId);
        }
        catch (SqlException ex) when (SqlErrors.IsUniqueViolation(ex))
        {
            return ServiceResult.Fail("That email or username is already taken.", nameof(vm.Username));
        }
    }
}
