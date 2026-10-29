using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;

namespace FootballClubManagement.Services;

public sealed class TeamHistoryService
{
    private readonly TeamHistoryRepository _histories;
    private readonly PersonRepository _persons;

    public TeamHistoryService(TeamHistoryRepository histories, PersonRepository persons)
    {
        _histories = histories;
        _persons = persons;
    }

    public Task<List<TeamHistoryRow>> ListAsync(int clubId) => _histories.ListByClubAsync(clubId);

    public Task<TeamHistoryRow?> GetAsync(int historyId, int clubId) => _histories.GetAsync(historyId, clubId);

    public async Task<TeamHistoryVm?> GetEditVmAsync(int historyId, int clubId)
    {
        var row = await _histories.GetAsync(historyId, clubId);
        if (row is null) return null;

        return new TeamHistoryVm
        {
            HistoryId = row.HistoryId,
            Title = row.Title,
            Description = row.Description,
            Year = row.Year
        };
    }

    public async Task<ServiceResult> CreateAsync(TeamHistoryVm vm, int personId, int clubId)
    {
        var coordinatorId = await _persons.GetCoordinatorIdAsync(personId);
        if (coordinatorId is null) return ServiceResult.Fail("Only a club coordinator can add club history.");

        var id = await _histories.CreateAsync(coordinatorId.Value, clubId, vm.Title, vm.Description, vm.Year);
        return ServiceResult.Success(id);
    }

    public async Task<ServiceResult> UpdateAsync(TeamHistoryVm vm, int personId, int clubId)
    {
        if (vm.HistoryId is null) return ServiceResult.Fail("That history entry could not be found.");

        var coordinatorId = await _persons.GetCoordinatorIdAsync(personId);
        if (coordinatorId is null) return ServiceResult.Fail("Only a club coordinator can edit club history.");

        var existing = await _histories.GetAsync(vm.HistoryId.Value, clubId);
        if (existing is null) return ServiceResult.Fail("That history entry does not belong to your club.");

        var updated = await _histories.UpdateAsync(vm.HistoryId.Value, clubId, vm.Title, vm.Description, vm.Year);
        return updated
            ? ServiceResult.Success(vm.HistoryId)
            : ServiceResult.Fail("That history entry could not be updated.");
    }

    public async Task<ServiceResult> DeleteAsync(int historyId, int personId, int clubId)
    {
        var coordinatorId = await _persons.GetCoordinatorIdAsync(personId);
        if (coordinatorId is null) return ServiceResult.Fail("Only a club coordinator can delete club history.");

        var existing = await _histories.GetAsync(historyId, clubId);
        if (existing is null) return ServiceResult.Fail("That history entry does not belong to your club.");

        var deleted = await _histories.DeleteAsync(historyId, clubId);
        return deleted
            ? ServiceResult.Success(historyId)
            : ServiceResult.Fail("That history entry could not be deleted.");
    }
}
