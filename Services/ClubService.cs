using FootballClubManagement.Data;
using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Services;

public sealed class ClubService
{
    private readonly ClubRepository _clubs;
    private readonly LocationRepository _locations;

    public ClubService(ClubRepository clubs, LocationRepository locations)
    {
        _clubs = clubs;
        _locations = locations;
    }

    public Task<List<ClubRow>> ListAsync() => _clubs.GetClubsAsync();

    public Task<ClubRow?> GetAsync(int clubId) => _clubs.GetClubAsync(clubId);

    public Task<ClubOverview?> OverviewAsync(int clubId) => _clubs.GetOverviewAsync(clubId);

    public async Task<ServiceResult> CreateAsync(ClubCreateVm vm)
    {
        var city = await _locations.GetCityAsync(vm.CityId);
        if (city is null)
        {
            return ServiceResult.Fail("Choose a valid home city.", nameof(vm.CityId));
        }

        var stadium = await _locations.GetStadiumAsync(vm.StadiumId);
        if (stadium is null)
        {
            return ServiceResult.Fail("Choose a valid home stadium.", nameof(vm.StadiumId));
        }

        if (stadium.CityId != city.CityId)
        {
            return ServiceResult.Fail("The home stadium must be in the selected home city.", nameof(vm.StadiumId));
        }

        try
        {
            var clubId = await _clubs.CreateClubAsync(vm.ClubName, vm.FoundedYear, vm.Gender, vm.AgeGroup,
                vm.CityId, vm.StadiumId);
            return ServiceResult.Success(clubId);
        }
        catch (SqlException ex) when (SqlErrors.IsUniqueViolation(ex))
        {
            return ServiceResult.Fail("A club with that name already exists.", nameof(vm.ClubName));
        }
    }
}
