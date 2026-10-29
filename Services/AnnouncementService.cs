using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;

namespace FootballClubManagement.Services;

public sealed class AnnouncementService
{
    private readonly AnnouncementRepository _announcements;
    private readonly PersonRepository _persons;

    public AnnouncementService(AnnouncementRepository announcements, PersonRepository persons)
    {
        _announcements = announcements;
        _persons = persons;
    }

    public Task<List<AnnouncementRow>> ListAsync(int clubId) => _announcements.ListByClubAsync(clubId);

    public Task<int?> GetCoordinatorIdAsync(int personId) => _persons.GetCoordinatorIdAsync(personId);

    public async Task<AnnouncementRow?> GetOwnAsync(int announcementId, int personId, int clubId)
    {
        var coordinatorId = await _persons.GetCoordinatorIdAsync(personId);
        if (coordinatorId is null) return null;

        var row = await _announcements.GetAsync(announcementId, clubId);
        return row is not null && row.CoordinatorId == coordinatorId.Value ? row : null;
    }

    public Task<List<AnnouncementRow>> ListRecentAsync(int clubId, int take) =>
        ListRecentAsyncCore(clubId, take);

    private async Task<List<AnnouncementRow>> ListRecentAsyncCore(int clubId, int take)
    {
        var all = await _announcements.ListByClubAsync(clubId);
        return all.Take(take).ToList();
    }

    public async Task<AnnouncementVm?> GetEditVmAsync(int announcementId, int clubId)
    {
        var row = await _announcements.GetAsync(announcementId, clubId);
        if (row is null) return null;

        return new AnnouncementVm
        {
            AnnouncementId = row.AnnouncementId,
            Title = row.Title,
            Description = row.Description
        };
    }

    public async Task<ServiceResult> CreateAsync(AnnouncementVm vm, int personId, int clubId)
    {
        var coordinatorId = await _persons.GetCoordinatorIdAsync(personId);
        if (coordinatorId is null) return ServiceResult.Fail("Only a club coordinator can post announcements.");

        var id = await _announcements.CreateAsync(coordinatorId.Value, clubId, vm.Title, vm.Description);
        return ServiceResult.Success(id);
    }

    public async Task<ServiceResult> UpdateAsync(AnnouncementVm vm, int clubId)
    {
        if (vm.AnnouncementId is null) return ServiceResult.Fail("That announcement could not be found.");

        var existing = await _announcements.GetAsync(vm.AnnouncementId.Value, clubId);
        if (existing is null) return ServiceResult.Fail("That announcement does not belong to your club.");

        var updated = await _announcements.UpdateAsync(vm.AnnouncementId.Value, clubId, vm.Title, vm.Description);
        return updated
            ? ServiceResult.Success(vm.AnnouncementId)
            : ServiceResult.Fail("That announcement could not be updated.");
    }

    public async Task<ServiceResult> DeleteAsync(int announcementId, int personId, int clubId)
    {
        var coordinatorId = await _persons.GetCoordinatorIdAsync(personId);
        if (coordinatorId is null) return ServiceResult.Fail("Only a club coordinator can delete announcements.");

        var existing = await _announcements.GetAsync(announcementId, clubId);
        if (existing is null) return ServiceResult.Fail("That announcement does not belong to your club.");
        if (existing.CoordinatorId != coordinatorId.Value)
        {
            return ServiceResult.Fail("You can only delete announcements you posted.");
        }

        var deleted = await _announcements.DeleteAsync(announcementId, coordinatorId.Value, clubId);
        return deleted
            ? ServiceResult.Success(announcementId)
            : ServiceResult.Fail("That announcement could not be deleted.");
    }
}
