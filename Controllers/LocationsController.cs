using FootballClubManagement.Models;
using FootballClubManagement.Services;
using FootballClubManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballClubManagement.Controllers;

[Authorize(Roles = RoleNames.Coordinator)]
public sealed class LocationsController : AppController
{
    private readonly LocationService _locations;
    private readonly LookupService _lookups;

    public LocationsController(LocationService locations, LookupService lookups)
    {
        _locations = locations;
        _lookups = lookups;
    }

    [HttpGet]
    public async Task<IActionResult> Cities() => View(await _locations.CitiesAsync());

    [HttpGet]
    public async Task<IActionResult> City(int id)
    {
        var city = await _locations.CityAsync(id);
        if (city is null) return NotFound();

        ViewBag.Stadiums = (await _locations.StadiumsAsync()).Where(s => s.CityId == id).ToList();
        return View(city);
    }

    [HttpGet]
    public async Task<IActionResult> CreateCity()
    {
        await PopulateProvincesAsync();
        return View("CityForm", new CityFormVm());
    }

    [HttpPost]
    public async Task<IActionResult> CreateCity(CityFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateProvincesAsync();
            return View("CityForm", vm);
        }

        var result = await _locations.CreateCityAsync(vm);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateProvincesAsync();
            return View("CityForm", vm);
        }

        FlashSuccess($"{vm.CityName} was added.");
        return RedirectToAction(nameof(Cities));
    }

    [HttpGet]
    public async Task<IActionResult> EditCity(int id)
    {
        var city = await _locations.CityAsync(id);
        if (city is null) return NotFound();

        await PopulateProvincesAsync();
        return View("CityForm", new CityFormVm
        {
            CityId = city.CityId,
            CityName = city.CityName,
            ProvinceId = city.ProvinceId
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditCity(CityFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateProvincesAsync();
            return View("CityForm", vm);
        }

        var result = await _locations.UpdateCityAsync(vm);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateProvincesAsync();
            return View("CityForm", vm);
        }

        FlashSuccess($"{vm.CityName} was updated.");
        return RedirectToAction(nameof(City), new { id = vm.CityId });
    }

    [HttpGet]
    public async Task<IActionResult> Stadiums() => View(await _locations.StadiumsAsync());

    [HttpGet]
    public async Task<IActionResult> Stadium(int id)
    {
        var stadium = await _locations.StadiumAsync(id);
        if (stadium is null) return NotFound();

        return View(stadium);
    }

    [HttpGet]
    public async Task<IActionResult> CreateStadium(int? cityId)
    {
        await PopulateCitiesAsync();
        return View("StadiumForm", new StadiumFormVm { CityId = cityId ?? 0 });
    }

    [HttpPost]
    public async Task<IActionResult> CreateStadium(StadiumFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCitiesAsync();
            return View("StadiumForm", vm);
        }

        var result = await _locations.CreateStadiumAsync(vm);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateCitiesAsync();
            return View("StadiumForm", vm);
        }

        FlashSuccess($"{vm.StadiumName} was added.");
        return RedirectToAction(nameof(Stadiums));
    }

    [HttpGet]
    public async Task<IActionResult> EditStadium(int id)
    {
        var stadium = await _locations.StadiumAsync(id);
        if (stadium is null) return NotFound();

        await PopulateCitiesAsync();
        return View("StadiumForm", new StadiumFormVm
        {
            StadiumId = stadium.StadiumId,
            StadiumName = stadium.StadiumName,
            Capacity = stadium.Capacity,
            Surface = stadium.Surface,
            CityId = stadium.CityId
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditStadium(StadiumFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCitiesAsync();
            return View("StadiumForm", vm);
        }

        var result = await _locations.UpdateStadiumAsync(vm);
        if (!result.Ok)
        {
            ApplyErrors(result);
            await PopulateCitiesAsync();
            return View("StadiumForm", vm);
        }

        FlashSuccess($"{vm.StadiumName} was updated.");
        return RedirectToAction(nameof(Stadium), new { id = vm.StadiumId });
    }

    private async Task PopulateProvincesAsync() => ViewBag.Provinces = await _locations.ProvincesAsync();

    private async Task PopulateCitiesAsync() => ViewBag.Cities = await _lookups.CitiesAsync();
}
