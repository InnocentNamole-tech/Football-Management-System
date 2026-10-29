using FootballClubManagement.Data;
using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Services;

public sealed class LocationService
{
    private readonly LocationRepository _locations;
    private readonly LookupService _lookups;

    public LocationService(LocationRepository locations, LookupService lookups)
    {
        _locations = locations;
        _lookups = lookups;
    }

    public Task<List<CityRow>> CitiesAsync() => _locations.GetCitiesAsync();

    public Task<CityRow?> CityAsync(int cityId) => _locations.GetCityAsync(cityId);

    public Task<List<StadiumRow>> StadiumsAsync() => _locations.GetStadiumsAsync();

    public Task<StadiumRow?> StadiumAsync(int stadiumId) => _locations.GetStadiumAsync(stadiumId);

    public Task<List<LookupItem>> ProvincesAsync() => _lookups.ProvincesAsync();

    public async Task<ServiceResult> CreateCityAsync(CityFormVm vm)
    {
        if (!await ProvinceExistsAsync(vm.ProvinceId))
        {
            return ServiceResult.Fail("Choose a valid province.", nameof(vm.ProvinceId));
        }

        try
        {
            var id = await _locations.CreateCityAsync(vm.CityName, vm.ProvinceId);
            return ServiceResult.Success(id);
        }
        catch (SqlException ex) when (SqlErrors.IsUniqueViolation(ex))
        {
            return ServiceResult.Fail("That city already exists in the selected province.", nameof(vm.CityName));
        }
    }

    public async Task<ServiceResult> UpdateCityAsync(CityFormVm vm)
    {
        if (await _locations.GetCityAsync(vm.CityId) is null)
        {
            return ServiceResult.Fail("That city no longer exists.");
        }

        if (!await ProvinceExistsAsync(vm.ProvinceId))
        {
            return ServiceResult.Fail("Choose a valid province.", nameof(vm.ProvinceId));
        }

        try
        {
            await _locations.UpdateCityAsync(vm.CityId, vm.CityName, vm.ProvinceId);
            return ServiceResult.Success(vm.CityId);
        }
        catch (SqlException ex) when (SqlErrors.IsUniqueViolation(ex))
        {
            return ServiceResult.Fail("That city already exists in the selected province.", nameof(vm.CityName));
        }
    }

    public async Task<ServiceResult> CreateStadiumAsync(StadiumFormVm vm)
    {
        if (await _locations.GetCityAsync(vm.CityId) is null)
        {
            return ServiceResult.Fail("Choose a valid city.", nameof(vm.CityId));
        }

        try
        {
            var id = await _locations.CreateStadiumAsync(vm.StadiumName, vm.Capacity, vm.Surface, vm.CityId);
            return ServiceResult.Success(id);
        }
        catch (SqlException ex) when (SqlErrors.IsUniqueViolation(ex))
        {
            return ServiceResult.Fail("That stadium already exists in the selected city.", nameof(vm.StadiumName));
        }
    }

    public async Task<ServiceResult> UpdateStadiumAsync(StadiumFormVm vm)
    {
        if (await _locations.GetStadiumAsync(vm.StadiumId) is null)
        {
            return ServiceResult.Fail("That stadium no longer exists.");
        }

        if (await _locations.GetCityAsync(vm.CityId) is null)
        {
            return ServiceResult.Fail("Choose a valid city.", nameof(vm.CityId));
        }

        try
        {
            await _locations.UpdateStadiumAsync(vm.StadiumId, vm.StadiumName, vm.Capacity, vm.Surface, vm.CityId);
            return ServiceResult.Success(vm.StadiumId);
        }
        catch (SqlException ex) when (SqlErrors.IsUniqueViolation(ex))
        {
            return ServiceResult.Fail("That stadium already exists in the selected city.", nameof(vm.StadiumName));
        }
    }

    private async Task<bool> ProvinceExistsAsync(int provinceId) =>
        (await _lookups.ProvincesAsync()).Any(p => p.Id == provinceId);
}
