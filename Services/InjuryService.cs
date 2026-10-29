using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;

namespace FootballClubManagement.Services;

public sealed class InjuryService
{
    private readonly InjuryRepository _injuries;
    private readonly PlayerRepository _players;

    public InjuryService(InjuryRepository injuries, PlayerRepository players)
    {
        _injuries = injuries;
        _players = players;
    }

    public Task<List<InjuryRow>> ListForClubAsync(int clubId) => _injuries.ListByClubAsync(clubId);

    public async Task<List<InjuryRow>> ListForPlayerAsync(int personId)
    {
        var playerId = await _players.GetPlayerIdByPersonAsync(personId);
        return playerId is null ? new List<InjuryRow>() : await _injuries.ListByPlayerAsync(playerId.Value);
    }

    public async Task<InjuryVm?> GetEditVmAsync(int injuryId, int clubId)
    {
        var injury = await _injuries.GetAsync(injuryId, clubId);
        if (injury is null) return null;

        return new InjuryVm
        {
            InjuryHistoryId = injury.InjuryHistoryId,
            PlayerId = injury.PlayerId,
            InjuryType = injury.InjuryType,
            InjuryDate = injury.InjuryDate,
            EstimatedRecovery = injury.EstimatedRecovery,
            ActualRecovery = injury.ActualRecovery
        };
    }

    public async Task<ServiceResult> CreateAsync(InjuryVm vm, int clubId)
    {
        var belongs = await _players.BelongsToClubAsync(vm.PlayerId, clubId);
        if (!belongs) return ServiceResult.Fail("That player is not active for your club.", nameof(vm.PlayerId));

        var injuryId = await _injuries.CreateAsync(vm.PlayerId, vm.InjuryType, vm.InjuryDate, vm.EstimatedRecovery);
        return ServiceResult.Success(injuryId);
    }

    public async Task<ServiceResult> UpdateAsync(InjuryVm vm, int clubId)
    {
        if (vm.InjuryHistoryId is null) return ServiceResult.Fail("That injury record could not be found.");

        var injury = await _injuries.GetAsync(vm.InjuryHistoryId.Value, clubId);
        if (injury is null) return ServiceResult.Fail("That injury record does not belong to your club.");

        var updated = await _injuries.UpdateAsync(
            vm.InjuryHistoryId.Value, clubId, vm.InjuryType, vm.InjuryDate, vm.EstimatedRecovery, vm.ActualRecovery);

        return updated
            ? ServiceResult.Success(vm.InjuryHistoryId)
            : ServiceResult.Fail("That injury record could not be updated.");
    }
}
