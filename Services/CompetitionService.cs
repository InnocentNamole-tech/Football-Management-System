using FootballClubManagement.Data;
using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Services;

public sealed class CompetitionService
{
    private static readonly string[] CompetitionTypes = { "Friendly", "League", "Cup", "Tournament", "Exhibition" };
    private static readonly string[] Genders = { "Men", "Women", "Mixed" };
    private static readonly string[] AgeGroups = { "U13", "U15", "U17", "U19", "Senior" };
    private static readonly string[] Statuses = { "Upcoming", "In Progress", "Completed" };
    private static readonly string[] YesNo = { "Yes", "No" };
    private static readonly string[] Outcomes = { "In Progress", "Won", "Lost" };

    private readonly CompetitionRepository _competitions;
    private readonly PersonRepository _persons;
    private readonly ClubRepository _clubs;

    public CompetitionService(CompetitionRepository competitions, PersonRepository persons, ClubRepository clubs)
    {
        _competitions = competitions;
        _persons = persons;
        _clubs = clubs;
    }

    public async Task<List<CompetitionRow>> ListForOrganiserAsync(int personId)
    {
        var organiserId = await _persons.GetOrganiserIdAsync(personId);
        return organiserId is null
            ? new List<CompetitionRow>()
            : await _competitions.ListForOrganiserAsync(organiserId.Value);
    }

    public Task<CompetitionRow?> GetAsync(int competitionId) => _competitions.GetAsync(competitionId);

    public Task<CompetitionRow?> GetForClubAsync(int competitionId, int clubId) =>
        _competitions.GetForClubAsync(competitionId, clubId);

    public async Task<List<CompetitionRow>> MatchingForClubAsync(int clubId)
    {
        var club = await _clubs.GetClubAsync(clubId);
        return club is null
            ? new List<CompetitionRow>()
            : await _competitions.ListMatchingForClubAsync(clubId, club.Gender, club.AgeGroup);
    }

    public Task<List<CompetitionEntryRow>> ListEntriesAsync(int clubId, string? status = null) =>
        _competitions.ListEntriesAsync(clubId, status);

    public async Task<CompetitionVm?> GetEditVmAsync(int competitionId, int personId)
    {
        var organiserId = await _persons.GetOrganiserIdAsync(personId);
        var competition = await _competitions.GetAsync(competitionId);
        if (organiserId is null || competition is null || competition.OrganiserId != organiserId.Value) return null;

        return new CompetitionVm
        {
            CompetitionId = competition.CompetitionId,
            CompetitionName = competition.CompetitionName,
            CompetitionType = competition.CompetitionType,
            Gender = competition.Gender,
            AgeGroup = competition.AgeGroup,
            StadiumId = competition.StadiumId,
            StartDate = competition.StartDate,
            StartTime = TimeOnly.FromTimeSpan(competition.StartTime),
            EndDate = competition.EndDate,
            TeamCount = competition.TeamCount,
            AffiliationFee = competition.AffiliationFee,
            RewardPrice = competition.RewardPrice,
            IndividualAward = competition.IndividualAward,
            TrophyMedal = competition.TrophyMedal,
            Promotion = competition.Promotion,
            KitSet = competition.KitSet,
            CompetitionStatus = competition.CompetitionStatus
        };
    }

    public async Task<ServiceResult> CreateAsync(CompetitionVm vm, int personId)
    {
        var organiserId = await _persons.GetOrganiserIdAsync(personId);
        if (organiserId is null) return ServiceResult.Fail("Only an organiser can create competitions.");

        var validation = Validate(vm);
        if (!validation.Ok) return validation;

        var input = ToInput(vm);
        var id = await _competitions.CreateAsync(organiserId.Value, input);
        return ServiceResult.Success(id);
    }

    public async Task<ServiceResult> UpdateAsync(CompetitionVm vm, int personId)
    {
        if (vm.CompetitionId is null) return ServiceResult.Fail("That competition could not be found.");

        var organiserId = await _persons.GetOrganiserIdAsync(personId);
        if (organiserId is null) return ServiceResult.Fail("Only an organiser can edit competitions.");

        var existing = await _competitions.GetAsync(vm.CompetitionId.Value);
        if (existing is null || existing.OrganiserId != organiserId.Value)
        {
            return ServiceResult.Fail("You can only edit your own competitions.");
        }

        var validation = Validate(vm);
        if (!validation.Ok) return validation;

        var updated = await _competitions.UpdateAsync(vm.CompetitionId.Value, organiserId.Value, ToInput(vm));
        return updated
            ? ServiceResult.Success(vm.CompetitionId)
            : ServiceResult.Fail("That competition could not be updated.");
    }

    private static ServiceResult Validate(CompetitionVm vm)
    {
        if (!CompetitionTypes.Contains(vm.CompetitionType)) return ServiceResult.Fail("Choose a valid competition type.", nameof(vm.CompetitionType));
        if (!Genders.Contains(vm.Gender)) return ServiceResult.Fail("Choose a valid gender.", nameof(vm.Gender));
        if (!AgeGroups.Contains(vm.AgeGroup)) return ServiceResult.Fail("Choose a valid age group.", nameof(vm.AgeGroup));
        if (!Statuses.Contains(vm.CompetitionStatus)) return ServiceResult.Fail("Choose a valid status.", nameof(vm.CompetitionStatus));
        if (!YesNo.Contains(vm.IndividualAward) || !YesNo.Contains(vm.TrophyMedal)
            || !YesNo.Contains(vm.Promotion) || !YesNo.Contains(vm.KitSet))
        {
            return ServiceResult.Fail("Reward options must be Yes or No.");
        }

        if (vm.EndDate < vm.StartDate) return ServiceResult.Fail("End date cannot be before the start date.", nameof(vm.EndDate));
        if (vm.TeamCount < 2) return ServiceResult.Fail("A competition needs at least two teams.", nameof(vm.TeamCount));
        if (vm.AffiliationFee < 0) return ServiceResult.Fail("Affiliation fee cannot be negative.", nameof(vm.AffiliationFee));
        if (vm.RewardPrice < 0) return ServiceResult.Fail("Reward price cannot be negative.", nameof(vm.RewardPrice));

        return ServiceResult.Success();
    }

    private static CompetitionInput ToInput(CompetitionVm vm)
    {
        var isFriendly = vm.CompetitionType == "Friendly";

        return new CompetitionInput
        {
            CompetitionName = vm.CompetitionName.Trim(),
            CompetitionType = vm.CompetitionType,
            Gender = vm.Gender,
            AgeGroup = vm.AgeGroup,
            StadiumId = vm.StadiumId,
            StartDate = vm.StartDate,
            StartTime = vm.StartTime.ToTimeSpan(),
            EndDate = vm.EndDate,
            TeamCount = vm.TeamCount,
            AffiliationFee = vm.AffiliationFee,
            RewardPrice = isFriendly ? 0 : vm.RewardPrice,
            IndividualAward = isFriendly ? "No" : vm.IndividualAward,
            TrophyMedal = isFriendly ? "No" : vm.TrophyMedal,
            Promotion = isFriendly ? "No" : vm.Promotion,
            KitSet = isFriendly ? "No" : vm.KitSet,
            CompetitionStatus = vm.CompetitionStatus
        };
    }

    public async Task<ServiceResult> RequestToJoinAsync(int competitionId, int clubId)
    {
        var competition = await _competitions.GetAsync(competitionId);
        var club = await _clubs.GetClubAsync(clubId);

        if (competition is null) return ServiceResult.Fail("That competition no longer exists.");
        if (club is null) return ServiceResult.Fail("Your club could not be found.");

        if (competition.AgeGroup != club.AgeGroup)
        {
            return ServiceResult.Fail("Your club's age group does not match this competition.");
        }

        if (competition.Gender != club.Gender && competition.Gender != "Mixed")
        {
            return ServiceResult.Fail("Your club's gender does not match this competition.");
        }

        var existing = await _competitions.GetForClubAsync(competitionId, clubId);
        if (existing?.EntryId is not null)
        {
            return ServiceResult.Fail("Your club has already requested to join this competition.");
        }

        try
        {
            await _competitions.RequestToJoinAsync(competitionId, clubId);
            return ServiceResult.Success(competitionId);
        }
        catch (SqlException ex) when (SqlErrors.IsUniqueViolation(ex))
        {
            return ServiceResult.Fail("Your club has already requested to join this competition.");
        }
    }

    public async Task<List<CompetitionEntryRow>> RequestsAsync(int personId, string? status = null)
    {
        var organiserId = await _persons.GetOrganiserIdAsync(personId);
        return organiserId is null
            ? new List<CompetitionEntryRow>()
            : await _competitions.ListEntriesForOrganiserAsync(organiserId.Value, status);
    }

    public async Task<List<CompetitionEntryRow>> ApprovedOutcomesAsync(int personId)
    {
        var organiserId = await _persons.GetOrganiserIdAsync(personId);
        return organiserId is null
            ? new List<CompetitionEntryRow>()
            : await _competitions.ListEntriesForOrganiserAsync(organiserId.Value, "Approved");
    }

    public async Task<ServiceResult> SetRequestStatusAsync(int entryId, int personId, string status)
    {
        if (status is not ("Approved" or "Rejected"))
        {
            return ServiceResult.Fail("Choose Approve or Reject.");
        }

        var organiserId = await _persons.GetOrganiserIdAsync(personId);
        if (organiserId is null) return ServiceResult.Fail("Only an organiser can review requests.");

        var entry = await _competitions.GetEntryForOrganiserAsync(entryId, organiserId.Value);
        if (entry is null) return ServiceResult.Fail("That request does not belong to your competitions.");
        if (entry.RequestStatus != "Pending") return ServiceResult.Fail("That request has already been reviewed.");

        var updated = await _competitions.UpdateRequestStatusAsync(entryId, organiserId.Value, status);
        return updated
            ? ServiceResult.Success(entryId)
            : ServiceResult.Fail("That request could not be updated.");
    }

    public async Task<OutcomeVm?> GetOutcomeVmAsync(int entryId, int personId)
    {
        var organiserId = await _persons.GetOrganiserIdAsync(personId);
        if (organiserId is null) return null;

        var entry = await _competitions.GetEntryForOrganiserAsync(entryId, organiserId.Value);
        if (entry is null) return null;

        return new OutcomeVm
        {
            CompetitionClubId = entry.CompetitionClubId,
            CompetitionName = entry.CompetitionName,
            ClubName = entry.ClubName,
            Outcome = entry.Outcome ?? "",
            StageReached = entry.StageReached
        };
    }

    public async Task<ServiceResult> RecordOutcomeAsync(OutcomeVm vm, int personId)
    {
        var organiserId = await _persons.GetOrganiserIdAsync(personId);
        if (organiserId is null) return ServiceResult.Fail("Only an organiser can record outcomes.");

        var entry = await _competitions.GetEntryForOrganiserAsync(vm.CompetitionClubId, organiserId.Value);
        if (entry is null) return ServiceResult.Fail("That entry does not belong to your competitions.");
        if (entry.RequestStatus != "Approved") return ServiceResult.Fail("Only approved clubs can receive an outcome.");
        if (!Outcomes.Contains(vm.Outcome)) return ServiceResult.Fail("Choose In Progress, Won or Lost.", nameof(vm.Outcome));

        string? stage = null;
        if (vm.Outcome is "In Progress" or "Lost")
        {
            if (string.IsNullOrWhiteSpace(vm.StageReached) || !Stages.All.Contains(vm.StageReached))
            {
                return ServiceResult.Fail("Choose the stage reached for that outcome.", nameof(vm.StageReached));
            }

            stage = vm.StageReached;
        }

        var updated = await _competitions.RecordOutcomeAsync(vm.CompetitionClubId, organiserId.Value, vm.Outcome, stage);
        return updated
            ? ServiceResult.Success(vm.CompetitionClubId)
            : ServiceResult.Fail("That outcome could not be saved.");
    }

    public async Task<int> PendingCountAsync(int personId)
    {
        var organiserId = await _persons.GetOrganiserIdAsync(personId);
        return organiserId is null ? 0 : await _competitions.CountPendingForOrganiserAsync(organiserId.Value);
    }

    public async Task<List<CompetitionEntryRow>> PendingAsync(int personId)
    {
        var organiserId = await _persons.GetOrganiserIdAsync(personId);
        return organiserId is null
            ? new List<CompetitionEntryRow>()
            : await _competitions.ListEntriesForOrganiserAsync(organiserId.Value, "Pending");
    }

    public Task<List<LookupItem>> ApprovedOptionsAsync(int clubId) => _competitions.GetApprovedOptionsAsync(clubId);
}
