using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;

namespace FootballClubManagement.Services;

public sealed class CommentService
{
    private readonly CommentRepository _comments;
    private readonly MatchRepository _matches;

    public CommentService(CommentRepository comments, MatchRepository matches)
    {
        _comments = comments;
        _matches = matches;
    }

    public Task<List<CommentRow>> ListAsync(int clubId) => _comments.ListByClubAsync(clubId);

    public Task<List<LookupItem>> GetMatchOptionsAsync(int clubId) => _matches.GetMatchOptionsAsync(clubId);

    public async Task<CommentRow?> GetOwnAsync(int commentId, int personId, int clubId)
    {
        var row = await _comments.GetAsync(commentId);
        return row is not null && row.PersonId == personId && row.ClubId == clubId ? row : null;
    }

    public async Task<CommentVm?> GetEditVmAsync(int commentId, int personId, int clubId)
    {
        var row = await _comments.GetAsync(commentId);
        if (row is null || row.PersonId != personId || row.ClubId != clubId) return null;

        return new CommentVm
        {
            CommentId = row.CommentId,
            MatchId = row.MatchId,
            CommentText = row.CommentText
        };
    }

    public async Task<ServiceResult> CreateAsync(CommentVm vm, int personId, int clubId)
    {
        var match = await _matches.GetAsync(vm.MatchId, clubId);
        if (match is null) return ServiceResult.Fail("That match does not belong to your club.", nameof(vm.MatchId));

        var id = await _comments.CreateAsync(vm.MatchId, personId, vm.CommentText);
        return ServiceResult.Success(id);
    }

    public async Task<ServiceResult> UpdateAsync(CommentVm vm, int personId, int clubId)
    {
        if (vm.CommentId is null) return ServiceResult.Fail("That comment could not be found.");

        var existing = await _comments.GetAsync(vm.CommentId.Value);
        if (existing is null || existing.PersonId != personId || existing.ClubId != clubId)
        {
            return ServiceResult.Fail("You can only edit your own comments.");
        }

        var updated = await _comments.UpdateAsync(vm.CommentId.Value, personId, vm.CommentText);
        return updated
            ? ServiceResult.Success(vm.CommentId)
            : ServiceResult.Fail("That comment could not be updated.");
    }

    public async Task<ServiceResult> DeleteAsync(int commentId, int personId, int clubId)
    {
        var existing = await _comments.GetAsync(commentId);
        if (existing is null || existing.PersonId != personId || existing.ClubId != clubId)
        {
            return ServiceResult.Fail("You can only delete your own comments.");
        }

        var deleted = await _comments.DeleteAsync(commentId, personId);
        return deleted
            ? ServiceResult.Success(commentId)
            : ServiceResult.Fail("That comment could not be deleted.");
    }
}
