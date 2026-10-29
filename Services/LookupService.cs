using FootballClubManagement.Models;
using FootballClubManagement.Repositories;

namespace FootballClubManagement.Services;

public sealed class LookupService
{
    private readonly ClubRepository _clubs;

    public LookupService(ClubRepository clubs) => _clubs = clubs;

    public Task<List<ClubRow>> ClubsAsync() => _clubs.GetClubsAsync();

    public Task<ClubRow?> ClubAsync(int clubId) => _clubs.GetClubAsync(clubId);

    public Task<List<LookupItem>> CitiesAsync() => _clubs.GetCitiesAsync();

    public Task<List<LookupItem>> ProvincesAsync() => _clubs.GetProvincesAsync();

    public Task<List<LookupItem>> StadiumsAsync() => _clubs.GetStadiumsAsync();

    public Task<List<LookupItem>> PositionsAsync() => _clubs.GetPositionsAsync();

    public Task<List<LookupItem>> SpecializationsAsync() => _clubs.GetSpecializationsAsync();

    public Task<List<LookupItem>> PlayerStylesAsync() => _clubs.GetPlayerStylesAsync();
}
