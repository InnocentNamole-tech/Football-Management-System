namespace FootballClubManagement.Models;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Organiser = "Organiser";
    public const string Coordinator = "Coordinator";
    public const string Coach = "Coach";
    public const string Player = "Player";
    public const string Fan = "Fan";

    public static readonly string[] All = { Admin, Organiser, Coordinator, Coach, Player, Fan };
}

public static class ValidationPatterns
{
    public const string Name = @"^[A-Za-z]+( [A-Za-z]+)*$";
    public const string Phone = @"^0\d{9}$";
    public const string Username = @"^[A-Za-z0-9_]{4,20}$";
    public const string Password = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,64}$";
}

public static class Formations
{
    public static readonly string[] All =
    {
        "4-4-2", "4-3-3", "4-2-3-1", "3-5-2", "3-4-3", "5-3-2", "4-5-1", "4-1-4-1"
    };
}

public static class CoachLicenseLevels
{
    public static readonly string[] All = { "SAFA A", "SAFA B", "SAFA C", "SAFA D" };
}

public static class ClubLookups
{
    public static readonly string[] Genders = { "Men", "Women", "Mixed" };
    public static readonly string[] AgeGroups = { "U13", "U15", "U17", "U19", "Senior" };
}

public static class StadiumSurfaces
{
    public static readonly string[] All = { "Grass", "Artificial", "Hybrid" };
}

public static class Stages
{
    public static readonly string[] All =
    {
        "Group Stage", "Round of 16", "Quarter-final", "Semi-final", "Final"
    };
}

public sealed class ServiceResult
{
    private ServiceResult(bool ok, string? message, string? field, int? id)
    {
        Ok = ok;
        Message = message;
        Field = field;
        Id = id;
    }

    public bool Ok { get; }
    public string? Message { get; }
    public string? Field { get; }
    public int? Id { get; }

    public static ServiceResult Success(int? id = null) => new(true, null, null, id);

    public static ServiceResult Fail(string message, string? field = null) => new(false, message, field, null);
}

public sealed class LookupItem
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}
