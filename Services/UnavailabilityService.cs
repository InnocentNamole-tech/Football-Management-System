using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;

namespace FootballClubManagement.Services;

public sealed class UnavailabilityService
{
    private readonly UnavailabilityRepository _unavailabilities;
    private readonly PlayerRepository _players;

    public UnavailabilityService(UnavailabilityRepository unavailabilities, PlayerRepository players)
    {
        _unavailabilities = unavailabilities;
        _players = players;
    }

    public Task<List<UnavailabilityRow>> ListForClubAsync(int clubId) => _unavailabilities.ListByClubAsync(clubId);

    public async Task<List<UnavailabilityRow>> ListForPlayerAsync(int personId)
    {
        var playerId = await _players.GetPlayerIdByPersonAsync(personId);
        return playerId is null ? new List<UnavailabilityRow>() : await _unavailabilities.ListByPlayerAsync(playerId.Value);
    }

    public async Task<UnavailabilityVm?> GetEditVmAsync(int id, int personId)
    {
        var playerId = await _players.GetPlayerIdByPersonAsync(personId);
        if (playerId is null) return null;

        var row = await _unavailabilities.GetForPlayerAsync(id, playerId.Value);
        if (row is null) return null;

        return new UnavailabilityVm
        {
            UnavailabilityId = row.UnavailabilityId,
            PlayerName = row.PlayerName,
            StartDate = row.StartDate,
            EndDate = row.EndDate,
            Reason = row.Reason
        };
    }

    public async Task<UnavailabilityRow?> GetOwnAsync(int id, int personId)
    {
        var playerId = await _players.GetPlayerIdByPersonAsync(personId);
        if (playerId is null) return null;
        return await _unavailabilities.GetForPlayerAsync(id, playerId.Value);
    }

    public async Task<ServiceResult> CreateAsync(UnavailabilityVm vm, int personId)
    {
        if (vm.EndDate < vm.StartDate)
        {
            return ServiceResult.Fail("End date cannot be before the start date.", nameof(vm.EndDate));
        }

        var playerId = await _players.GetPlayerIdByPersonAsync(personId);
        if (playerId is null) return ServiceResult.Fail("Your player profile could not be found.");

        var id = await _unavailabilities.CreateAsync(playerId.Value, vm.StartDate, vm.EndDate, vm.Reason);
        return ServiceResult.Success(id);
    }

    public async Task<ServiceResult> UpdateAsync(UnavailabilityVm vm, int personId)
    {
        if (vm.UnavailabilityId is null) return ServiceResult.Fail("That unavailability record could not be found.");
        if (vm.EndDate < vm.StartDate)
        {
            return ServiceResult.Fail("End date cannot be before the start date.", nameof(vm.EndDate));
        }

        var playerId = await _players.GetPlayerIdByPersonAsync(personId);
        if (playerId is null) return ServiceResult.Fail("Your player profile could not be found.");

        var updated = await _unavailabilities.UpdateAsync(
            vm.UnavailabilityId.Value, playerId.Value, vm.StartDate, vm.EndDate, vm.Reason);

        return updated
            ? ServiceResult.Success(vm.UnavailabilityId)
            : ServiceResult.Fail("That unavailability record does not belong to you.");
    }

    public async Task<ServiceResult> DeleteAsync(int unavailabilityId, int personId)
    {
        var playerId = await _players.GetPlayerIdByPersonAsync(personId);
        if (playerId is null) return ServiceResult.Fail("Your player profile could not be found.");

        var deleted = await _unavailabilities.DeleteAsync(unavailabilityId, playerId.Value);
        return deleted
            ? ServiceResult.Success(unavailabilityId)
            : ServiceResult.Fail("You can only delete your own unavailability records.");
    }
}
