using FootballClubManagement.Data;
using FootballClubManagement.Models;
using FootballClubManagement.Repositories;
using FootballClubManagement.ViewModels;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Services;

public sealed class PlayerService
{
    private readonly PlayerRepository _players;
    private readonly PersonRepository _persons;

    public PlayerService(PlayerRepository players, PersonRepository persons)
    {
        _players = players;
        _persons = persons;
    }

    public Task<List<PlayerOverview>> ListAsync(int clubId, bool includeInactive = true) =>
        _players.ListByClubAsync(clubId, includeInactive);

    public Task<PlayerOverview?> GetAsync(int playerId, int clubId) => _players.GetAsync(playerId, clubId);

    public Task<PlayerOverview?> GetByPersonAsync(int personId) => _players.GetByPersonAsync(personId);

    public async Task<ServiceResult> CreateAsync(PlayerCreateVm vm, int clubId)
    {
        if (await _persons.EmailExistsAsync(vm.Email))
        {
            return ServiceResult.Fail("That email address is already registered.", nameof(vm.Email));
        }

        if (await _persons.UsernameExistsAsync(vm.Username))
        {
            return ServiceResult.Fail("That username is already taken.", nameof(vm.Username));
        }

        try
        {
            var playerId = await _players.CreateAsync(
                vm.FirstName, vm.LastName, vm.Email, vm.Username, vm.Phone, Passwords.Hash(vm.Password),
                vm.Nickname, vm.CityId, vm.PlayerStyleId, clubId, vm.PositionId, vm.JerseyNumber, vm.DateJoined);

            return ServiceResult.Success(playerId);
        }
        catch (SqlException ex) when (SqlErrors.IsUniqueViolation(ex))
        {
            if (ex.Message.Contains("PlayerClubs", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail("That jersey number is already used by an active player in this club.", nameof(vm.JerseyNumber));
            }

            return ServiceResult.Fail("That email or username is already taken.", nameof(vm.Username));
        }
    }

    public async Task<PlayerEditVm?> GetEditVmAsync(int playerId, int clubId)
    {
        var player = await _players.GetAsync(playerId, clubId);
        if (player is null) return null;

        return new PlayerEditVm
        {
            PlayerId = player.PlayerId,
            PersonId = player.PersonId,
            Username = player.Username,
            ClubName = player.ClubName,
            FirstName = player.FirstName,
            LastName = player.LastName,
            Email = player.Email,
            Phone = player.Phone,
            Nickname = player.Nickname,
            CityId = player.CityId,
            PositionId = player.PositionId,
            JerseyNumber = player.JerseyNumber,
            PlayerStyleId = player.PlayerStyleId
        };
    }

    public async Task<ServiceResult> UpdateAsync(PlayerEditVm vm, int clubId)
    {
        var player = await _players.GetAsync(vm.PlayerId, clubId);
        if (player is null) return ServiceResult.Fail("That player does not belong to your club.");

        if (await _persons.EmailExistsAsync(vm.Email, player.PersonId))
        {
            return ServiceResult.Fail("That email address is already registered.", nameof(vm.Email));
        }

        try
        {
            var updated = await _players.UpdateAsync(
                vm.PlayerId, clubId, vm.FirstName, vm.LastName, vm.Email, vm.Phone,
                vm.Nickname, vm.CityId, vm.PlayerStyleId, vm.PositionId, vm.JerseyNumber);

            return updated
                ? ServiceResult.Success(vm.PlayerId)
                : ServiceResult.Fail("That player is no longer active for your club.");
        }
        catch (SqlException ex) when (SqlErrors.IsUniqueViolation(ex))
        {
            if (ex.Message.Contains("PlayerClubs", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail("That jersey number is already used by an active player in this club.", nameof(vm.JerseyNumber));
            }

            return ServiceResult.Fail("That email address is already registered.", nameof(vm.Email));
        }
    }

    public async Task<ServiceResult> DeactivateAsync(int playerId, int clubId)
    {
        var player = await _players.GetAsync(playerId, clubId);
        if (player is null) return ServiceResult.Fail("That player does not belong to your club.");
        if (!player.IsActive) return ServiceResult.Fail("That player is already inactive.");

        var done = await _players.DeactivateAsync(playerId, clubId);
        return done
            ? ServiceResult.Success(playerId)
            : ServiceResult.Fail("That player could not be deactivated.");
    }
}
