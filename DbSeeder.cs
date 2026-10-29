using FootballClubManagement.Data;
using FootballClubManagement.Services;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement;

public sealed class DbSeeder
{
    private readonly Db _db;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(Db db, ILogger<DbSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await using var connection = await _db.OpenAsync();

        await using (var check = new SqlCommand("SELECT COUNT(*) FROM dbo.Persons", connection))
        {
            var count = (int)(await check.ExecuteScalarAsync() ?? 0);
            if (count > 0)
            {
                _logger.LogInformation("Seed data already present; skipping.");
                return;
            }
        }

        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            var provinceIds = new Dictionary<string, int>();
            foreach (var name in new[] { "Eastern Cape", "Limpopo" })
            {
                provinceIds[name] = await InsertAsync(connection, tx,
                    "INSERT INTO dbo.Provinces (ProvinceName) OUTPUT INSERTED.ProvinceId VALUES (@name)",
                    Db.P("@name", name));
            }

            var cityIds = new Dictionary<string, int>
            {
                ["East London"] = await InsertAsync(connection, tx,
                    "INSERT INTO dbo.Cities (CityName, ProvinceId) OUTPUT INSERTED.CityId VALUES (@name, @province)",
                    Db.P("@name", "East London"), Db.P("@province", provinceIds["Eastern Cape"])),
                ["Jane Furse"] = await InsertAsync(connection, tx,
                    "INSERT INTO dbo.Cities (CityName, ProvinceId) OUTPUT INSERTED.CityId VALUES (@name, @province)",
                    Db.P("@name", "Jane Furse"), Db.P("@province", provinceIds["Limpopo"]))
            };

            var stadiumIds = new Dictionary<string, int>
            {
                ["Buffalo City Stadium"] = await InsertStadiumAsync(connection, tx, "Buffalo City Stadium", 16000, "Grass", cityIds["East London"]),
                ["WSU BCC Park"] = await InsertStadiumAsync(connection, tx, "WSU BCC Park", 5000, "Grass", cityIds["East London"]),
                ["Dream Arena"] = await InsertStadiumAsync(connection, tx, "Dream Arena", 8000, "Hybrid", cityIds["East London"]),
                ["Jane Furse Stadium"] = await InsertStadiumAsync(connection, tx, "Jane Furse Stadium", 5000, "Artificial", cityIds["Jane Furse"])
            };

            var clubIds = new Dictionary<string, int>
            {
                ["WSU BCC"] = await InsertAsync(connection, tx,
                    @"INSERT INTO dbo.FootballClubs (ClubName, FoundedYear, Gender, AgeGroup, CityId, HomeStadiumId)
                      OUTPUT INSERTED.ClubId VALUES (@name, 1998, 'Men', 'Senior', @city, @stadium)",
                    Db.P("@name", "WSU BCC"), Db.P("@city", cityIds["East London"]), Db.P("@stadium", stadiumIds["WSU BCC Park"])),
                ["WSU Dream Team"] = await InsertAsync(connection, tx,
                    @"INSERT INTO dbo.FootballClubs (ClubName, FoundedYear, Gender, AgeGroup, CityId, HomeStadiumId)
                      OUTPUT INSERTED.ClubId VALUES (@name, 2005, 'Women', 'Senior', @city, @stadium)",
                    Db.P("@name", "WSU Dream Team"), Db.P("@city", cityIds["East London"]), Db.P("@stadium", stadiumIds["Dream Arena"])),
                ["Juniors FC"] = await InsertAsync(connection, tx,
                    @"INSERT INTO dbo.FootballClubs (ClubName, FoundedYear, Gender, AgeGroup, CityId, HomeStadiumId)
                      OUTPUT INSERTED.ClubId VALUES (@name, 2012, 'Men', 'U19', @city, @stadium)",
                    Db.P("@name", "Juniors FC Under 19"), Db.P("@city", cityIds["Jane Furse"]), Db.P("@stadium", stadiumIds["Jane Furse Stadium"]))
            };

            var clubKeys = new Dictionary<string, (string Suffix, string Domain)>
            {
                ["WSU BCC"] = ("bcc", "bcc.ac.za"),
                ["WSU Dream Team"] = ("dream", "dream.ac.za"),
                ["Juniors FC"] = ("juniors", "juniors.ac.za")
            };

            var positionIds = await InsertLookupAsync(connection, tx, "dbo.Positions", "PositionId",
                new[] { "Goalkeeper", "Right Back", "Centre Back", "Left Back", "Defensive Midfielder",
                        "Central Midfielder", "Attacking Midfielder", "Right Winger", "Left Winger", "Striker" });

            var specializationNames = new[] { "Head Coach", "Assistant Coach", "Goalkeeping Coach", "Fitness Coach" };
            var specializationIds = await InsertLookupAsync(connection, tx, "dbo.Specializations", "SpecializationId",
                specializationNames);

            var styleIds = (await InsertLookupAsync(connection, tx, "dbo.PlayerStyles", "PlayerStyleId",
                new[] { "Playmaker", "Poacher", "Engine", "Rock", "Speedster", "Target Man" })).Values.ToList();

            var hashes = new Dictionary<string, string>
            {
                ["Admin"] = Passwords.Hash("systemAdmin123!"),
                ["Organiser"] = Passwords.Hash("Organiser123!"),
                ["Coordinator"] = Passwords.Hash("Coordinator123!"),
                ["Coach"] = Passwords.Hash("Coach123!"),
                ["Player"] = Passwords.Hash("Player123!"),
                ["Fan"] = Passwords.Hash("clubFan123!")
            };

            var phoneCounter = 1;
            string NextPhone() => $"07{71000000 + phoneCounter++}";

            var usedUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string ClubUsername(string first, string club)
            {
                var root = new string(first.Where(char.IsLetter).ToArray()).ToLowerInvariant();
                var baseName = $"{root}_{clubKeys[club].Suffix}";
                var candidate = baseName.Length > 20 ? baseName[..20] : baseName;
                var suffixCounter = 2;
                while (usedUsernames.Contains(candidate))
                {
                    var tail = suffixCounter++.ToString();
                    candidate = $"{candidate[..Math.Min(candidate.Length, 20 - tail.Length)]}{tail}";
                }

                usedUsernames.Add(candidate);
                return candidate;
            }

            string ClubEmail(string first, string last, string club)
            {
                var initial = first.Length > 0 ? char.ToUpperInvariant(first[0]).ToString() : "X";
                var surname = new string(last.Where(char.IsLetter).ToArray());
                var local = $"{initial}{surname}";
                var candidate = $"{local}@{clubKeys[club].Domain}";
                var suffixCounter = 2;
                while (usedEmails.Contains(candidate))
                {
                    candidate = $"{local}{suffixCounter++}@{clubKeys[club].Domain}";
                }

                usedEmails.Add(candidate);
                return candidate;
            }

            async Task<int> AddPersonAsync(string first, string last, string username, string role, string? email = null)
            {
                usedUsernames.Add(username);
                usedEmails.Add(email ?? $"{username}@fcm.local");

                return await InsertAsync(connection, tx,
                    @"INSERT INTO dbo.Persons (FirstName, LastName, Email, Username, Phone, PasswordHash)
                      OUTPUT INSERTED.PersonId VALUES (@f, @l, @e, @u, @p, @h)",
                    Db.P("@f", first), Db.P("@l", last),
                    Db.P("@e", email ?? $"{username}@fcm.local"), Db.P("@u", username),
                    Db.P("@p", NextPhone()), Db.P("@h", hashes[role]));
            }

            var adminPersonId = await AddPersonAsync("Sipho", "Nkosi", "sysadmin", "Admin");
            await InsertAsync(connection, tx,
                "INSERT INTO dbo.Admins (PersonId) OUTPUT INSERTED.AdminId VALUES (@person)",
                Db.P("@person", adminPersonId));

            var organiserPersonIds = new List<int>();
            var organiserIds = new List<int>();
            var organiserNames = new[]
            {
                ("Thabo", "Mokoena", "organiser1"),
                ("Lerato", "Dlamini", "organiser2")
            };

            foreach (var (first, last, username) in organiserNames)
            {
                var personId = await AddPersonAsync(first, last, username, "Organiser");
                var organiserId = await InsertAsync(connection, tx,
                    "INSERT INTO dbo.Organisers (PersonId) OUTPUT INSERTED.OrganiserId VALUES (@person)",
                    Db.P("@person", personId));
                organiserPersonIds.Add(personId);
                organiserIds.Add(organiserId);
            }

            var coordinatorPersonIds = new Dictionary<string, int>();
            var coordinatorIds = new Dictionary<string, int>();
            var coordinatorNames = new (string First, string Last, string Club)[]
            {
                ("Naledi", "Khumalo", "WSU BCC"),
                ("Ayanda", "Botha", "WSU Dream Team"),
                ("Sibusiso", "Ndlovu", "Juniors FC")
            };

            foreach (var (first, last, club) in coordinatorNames)
            {
                var personId = await AddPersonAsync(
                    first, last, ClubUsername(first, club), "Coordinator", ClubEmail(first, last, club));
                var coordinatorId = await InsertAsync(connection, tx,
                    @"INSERT INTO dbo.Coordinators (PersonId, ClubId) OUTPUT INSERTED.CoordinatorId
                      VALUES (@person, @club)",
                    Db.P("@person", personId), Db.P("@club", clubIds[club]));

                coordinatorPersonIds[club] = personId;
                coordinatorIds[club] = coordinatorId;
            }

            var coachNames = new (string First, string Last, string User, string Club, int Spec, string License)[]
            {
                ("Kagiso", "Mahlangu", "coach_bcc", "WSU BCC", 0, "SAFA A"),
                ("Themba", "Radebe", "coach_bcc2", "WSU BCC", 1, "SAFA C"),
                ("Zanele", "Mthembu", "coach_dream", "WSU Dream Team", 0, "SAFA B"),
                ("Bongani", "Zulu", "coach_juniors", "Juniors FC", 0, "SAFA B")
            };

            var coachPersonIds = new List<int>();
            foreach (var (first, last, user, club, spec, license) in coachNames)
            {
                var personId = await AddPersonAsync(first, last, user, "Coach");
                var coachId = await InsertAsync(connection, tx,
                    "INSERT INTO dbo.Coaches (PersonId, LicenseLevel) OUTPUT INSERTED.CoachId VALUES (@person, @license)",
                    Db.P("@person", personId), Db.P("@license", license));
                await ExecAsync(connection, tx,
                    @"INSERT INTO dbo.CoachClubs (CoachId, ClubId, SpecializationId, DateJoined)
                      VALUES (@coach, @club, @spec, @joined)",
                    Db.P("@coach", coachId), Db.P("@club", clubIds[club]),
                    Db.P("@spec", specializationIds[specializationNames[spec]]), Db.P("@joined", DateTime.Today.AddYears(-2)));

                coachPersonIds.Add(personId);
            }

            var fanNames = new (string First, string Last, string Club)[]
            {
                ("Palesa", "Molefe", "WSU BCC"),
                ("Kabelo", "Modise", "WSU BCC"),
                ("Neo", "Sebola", "WSU BCC"),
                ("Refilwe", "Tau", "WSU Dream Team"),
                ("Karabo", "Adams", "WSU Dream Team"),
                ("Amogelang", "Naidoo", "WSU Dream Team"),
                ("Oarabile", "Pillay", "Juniors FC"),
                ("Keabetswe", "Govender", "Juniors FC"),
                ("Omphile", "Cele", "Juniors FC")
            };

            var fanPersonIds = new Dictionary<string, List<int>>
            {
                ["WSU BCC"] = new(),
                ["WSU Dream Team"] = new(),
                ["Juniors FC"] = new()
            };

            foreach (var (first, last, club) in fanNames)
            {
                var personId = await AddPersonAsync(
                    first, last, ClubUsername(first, club), "Fan", ClubEmail(first, last, club));
                var fanId = await InsertAsync(connection, tx,
                    "INSERT INTO dbo.Fans (PersonId) OUTPUT INSERTED.FanId VALUES (@person)",
                    Db.P("@person", personId));
                await ExecAsync(connection, tx,
                    "INSERT INTO dbo.FanClubs (FanId, ClubId, DateJoined) VALUES (@fan, @club, @joined)",
                    Db.P("@fan", fanId), Db.P("@club", clubIds[club]), Db.P("@joined", DateTime.Today.AddYears(-1)));

                fanPersonIds[club].Add(personId);
            }

            var firstNames = new[]
            {
                "Thabo", "Sipho", "Kagiso", "Lebo", "Tshepo", "Andile", "Bongani", "Sibusiso",
                "Katlego", "Mpho", "Tumelo", "Lindiwe", "Zanele", "Naledi", "Ayanda", "Lerato",
                "Thandi", "Nomvula", "Palesa", "Kabelo", "Neo", "Refilwe", "Tshegofatso", "Karabo",
                "Amogelang", "Oarabile", "Keabetswe", "Omphile", "Bokang", "Rethabile"
            };

            var lastNames = new[]
            {
                "Mokoena", "Dlamini", "Khumalo", "Nkosi", "Ndlovu", "Mahlangu", "Mthembu", "Zulu",
                "Radebe", "Sithole", "Maseko", "Molefe", "Modise", "Sebola", "Tau", "Hlatshwayo",
                "Jansen", "Fortuin", "Adams", "Naidoo", "Pillay", "Govender", "Cele", "Mkhize"
            };

            var nicknames = new[]
            {
                "The Captain", "Speedy", "Maestro", "The Wall", "Engine", "Fox",
                "Tank", "Artist", "Rocket", "General", "Sniper", "Bulldog",
                "Wizard", "Hammer", "Flash", "Professor", "Bull", "Silky",
                "Cannon", "Anchor", "Magician", "Rhino"
            };

            var bccJerseys = new[] { 9, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 };
            var bccPositions = new[]
            {
                "Striker", "Goalkeeper", "Right Back", "Centre Back", "Centre Back", "Left Back",
                "Defensive Midfielder", "Central Midfielder", "Attacking Midfielder", "Right Winger", "Left Winger",
                "Striker", "Central Midfielder", "Centre Back", "Right Back", "Left Back",
                "Defensive Midfielder", "Right Winger", "Left Winger", "Striker", "Central Midfielder", "Centre Back"
            };

            var standardJerseys = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var standardPositions = new[]
            {
                "Goalkeeper", "Right Back", "Centre Back", "Centre Back", "Left Back", "Defensive Midfielder",
                "Central Midfielder", "Attacking Midfielder", "Right Winger", "Left Winger", "Striker", "Striker"
            };

            var rosters = new Dictionary<string, List<(int PlayerId, int PersonId, int Jersey, string Position)>>();

            async Task<List<(int, int, int, string)>> AddClubPlayersAsync(
                string club, int count, int offset, bool demoUser, bool bcc)
            {
                var clubId = clubIds[club];
                var cityId = cityIds[club == "Juniors FC" ? "Jane Furse" : "East London"];
                var list = new List<(int, int, int, string)>();

                for (var i = 0; i < count; i++)
                {
                    var first = firstNames[(i + offset) % firstNames.Length];
                    var last = lastNames[(i * 5 + offset + 3) % lastNames.Length];
                    if (demoUser && i == 0)
                    {
                        first = "Karim";
                        last = "Jamal";
                    }

                    var personId = await AddPersonAsync(
                        first, last, ClubUsername(first, club), "Player", ClubEmail(first, last, club));

                    var playerId = await InsertAsync(connection, tx,
                        @"INSERT INTO dbo.Players (PersonId, Nickname, CityId, PlayerStyleId)
                          OUTPUT INSERTED.PlayerId VALUES (@person, @nick, @city, @style)",
                        Db.P("@person", personId),
                        Db.P("@nick", demoUser && i == 0 ? "The Captain" : nicknames[(i + offset) % nicknames.Length]),
                        Db.P("@city", cityId),
                        Db.P("@style", styleIds[(i + offset) % styleIds.Count]));

                    var position = bcc ? bccPositions[i] : standardPositions[i];
                    var jersey = bcc ? bccJerseys[i] : standardJerseys[i];
                    var active = !(bcc && i >= count - 2);

                    await ExecAsync(connection, tx,
                        @"INSERT INTO dbo.PlayerClubs (PlayerId, ClubId, PositionId, JerseyNumber, DateJoined, IsActive)
                          VALUES (@player, @club, @position, @jersey, @joined, @active)",
                        Db.P("@player", playerId), Db.P("@club", clubId),
                        Db.P("@position", positionIds[position]), Db.P("@jersey", jersey),
                        Db.P("@joined", DateTime.Today.AddYears(-(1 + (i % 3)))), Db.P("@active", active));

                    list.Add((playerId, personId, jersey, position));
                }

                return list;
            }

            rosters["WSU BCC"] = await AddClubPlayersAsync("WSU BCC", 22, 0, true, true);
            rosters["WSU Dream Team"] = await AddClubPlayersAsync("WSU Dream Team", 12, 5, false, false);
            rosters["Juniors FC"] = await AddClubPlayersAsync("Juniors FC", 12, 11, false, false);

            async Task<int> AddCompetitionAsync(
                string name, string type, string gender, string ageGroup, int organiserIndex, int stadiumId,
                DateTime start, DateTime end, int teams, decimal fee, int reward,
                string award, string medal, string promotion, string kit, string status)
            {
                return await InsertAsync(connection, tx,
                    @"INSERT INTO dbo.Competitions
                        (CompetitionName, CompetitionType, Gender, AgeGroup, OrganiserId, StadiumId,
                         StartDate, StartTime, EndDate, TeamCount, AffiliationFee, RewardPrice,
                         IndividualAward, TrophyMedal, Promotion, KitSet, CompetitionStatus)
                      OUTPUT INSERTED.CompetitionId
                      VALUES (@name, @type, @gender, @age, @organiser, @stadium,
                              @start, @time, @end, @teams, @fee, @reward,
                              @award, @medal, @promotion, @kit, @status)",
                    Db.P("@name", name), Db.P("@type", type), Db.P("@gender", gender), Db.P("@age", ageGroup),
                    Db.P("@organiser", organiserIds[organiserIndex]), Db.P("@stadium", stadiumId),
                    Db.P("@start", start), Db.P("@time", new TimeSpan(15, 0, 0)), Db.P("@end", end),
                    Db.P("@teams", teams), Db.P("@fee", fee), Db.P("@reward", reward),
                    Db.P("@award", award), Db.P("@medal", medal), Db.P("@promotion", promotion),
                    Db.P("@kit", kit), Db.P("@status", status));
            }

            var today = DateTime.Today;

            var premierLeague = await AddCompetitionAsync(
                "Eastern Cape Premier League", "League", "Men", "Senior", 0, stadiumIds["Buffalo City Stadium"],
                today.AddDays(14), today.AddDays(90), 10, 1500m, 50000, "Yes", "Yes", "Yes", "No", "Upcoming");

            var coastalCup = await AddCompetitionAsync(
                "Eastern Cape Cup", "Cup", "Men", "Senior", 0, stadiumIds["Buffalo City Stadium"],
                today.AddDays(-30), today.AddDays(20), 8, 800m, 25000, "Yes", "Yes", "No", "Yes", "In Progress");

            var summerFriendly = await AddCompetitionAsync(
                "WSU Summer Friendly", "Friendly", "Men", "Senior", 1, stadiumIds["WSU BCC Park"],
                today.AddDays(-40), today.AddDays(-38), 4, 0m, 0, "No", "No", "No", "No", "Completed");

            var juniorMasters = await AddCompetitionAsync(
                "Limpopo Junior Masters", "Tournament", "Men", "U19", 1, stadiumIds["Jane Furse Stadium"],
                today.AddDays(21), today.AddDays(45), 6, 500m, 10000, "Yes", "Yes", "No", "Yes", "Upcoming");

            var womenInvitational = await AddCompetitionAsync(
                "Women's Invitational", "Cup", "Women", "Senior", 0, stadiumIds["Dream Arena"],
                today.AddDays(-20), today.AddDays(40), 8, 750m, 20000, "Yes", "Yes", "Yes", "No", "In Progress");

            var dreamExhibition = await AddCompetitionAsync(
                "Dream Exhibition", "Exhibition", "Mixed", "Senior", 1, stadiumIds["Dream Arena"],
                today.AddDays(-25), today.AddDays(-10), 6, 300m, 5000, "No", "Yes", "No", "No", "Completed");

            var provincialTrophy = await AddCompetitionAsync(
                "Eastern Cape Provincial Trophy", "League", "Men", "Senior", 0, stadiumIds["WSU BCC Park"],
                today.AddDays(35), today.AddDays(100), 12, 1000m, 30000, "Yes", "No", "No", "Yes", "Upcoming");

            async Task AddRequestAsync(int competitionId, string club, string status, string? outcome, string? stage)
            {
                await ExecAsync(connection, tx,
                    @"INSERT INTO dbo.CompetitionClubs (CompetitionId, ClubId, RequestStatus, Outcome, StageReached)
                      VALUES (@comp, @club, @status, @outcome, @stage)",
                    Db.P("@comp", competitionId), Db.P("@club", clubIds[club]),
                    Db.P("@status", status), Db.P("@outcome", outcome), Db.P("@stage", stage));
            }

            await AddRequestAsync(premierLeague, "WSU BCC", "Approved", "In Progress", "Group Stage");
            await AddRequestAsync(coastalCup, "WSU BCC", "Approved", "Lost", "Semi-final");
            await AddRequestAsync(summerFriendly, "WSU BCC", "Approved", "Won", null);
            await AddRequestAsync(juniorMasters, "Juniors FC", "Approved", "In Progress", "Round of 16");
            await AddRequestAsync(womenInvitational, "WSU Dream Team", "Approved", "In Progress", "Group Stage");
            await AddRequestAsync(dreamExhibition, "WSU BCC", "Approved", "Lost", "Semi-final");
            await AddRequestAsync(dreamExhibition, "WSU Dream Team", "Approved", "In Progress", "Group Stage");
            await AddRequestAsync(provincialTrophy, "WSU BCC", "Pending", null, null);
            await AddRequestAsync(provincialTrophy, "WSU Dream Team", "Rejected", null, null);

            var approvedCompetitions = new Dictionary<string, List<int>>
            {
                ["WSU BCC"] = new() { premierLeague, coastalCup, summerFriendly, dreamExhibition },
                ["WSU Dream Team"] = new() { womenInvitational, dreamExhibition },
                ["Juniors FC"] = new() { juniorMasters }
            };

            var opponents = new[]
            {
                "East London Rovers", "Buffalo City United", "Nelson Mandela Bay Stars", "Queenstown Chiefs",
                "Mthatha City FC", "Jane Furse Eagles", "Polokwane Academy", "Northside Warriors"
            };

            var formations = new[] { "4-4-2", "4-3-3", "4-2-3-1", "3-5-2" };
            var results = new[] { "Win", "Win", "Loss", "Draw", "Win", "Loss", "Draw", "Win" };
            var clubCompletedMatches = new Dictionary<string, List<int>>();
            var clubUpcomingMatches = new Dictionary<string, List<int>>();
            var matchCounter = 0;

            foreach (var club in new[] { "WSU BCC", "WSU Dream Team", "Juniors FC" })
            {
                clubCompletedMatches[club] = new List<int>();
                clubUpcomingMatches[club] = new List<int>();

                for (var i = 0; i < 12; i++)
                {
                    var completed = i < 8;
                    var matchDate = completed ? today.AddDays(-70 + i * 8) : today.AddDays(7 + (i - 8) * 7);
                    var competitionId = i % 3 == 0 ? approvedCompetitions[club][i % approvedCompetitions[club].Count] : (int?)null;

                    string? result = null;
                    int? goalFor = null;
                    int? goalAgainst = null;

                    if (completed)
                    {
                        result = results[(i + matchCounter) % results.Length];
                        goalFor = result switch
                        {
                            "Win" => 2 + (i % 2),
                            "Loss" => i % 3,
                            _ => 1
                        };
                        goalAgainst = result switch
                        {
                            "Win" => 1,
                            "Loss" => goalFor + 1,
                            _ => goalFor
                        };
                    }

                    var matchId = await InsertAsync(connection, tx,
                        @"INSERT INTO dbo.Matches
                            (ClubId, CompetitionId, Opponent, MatchDate, KickoffTime, StadiumId, Formation, Result, GoalFor, GoalAgainst)
                          OUTPUT INSERTED.MatchId
                          VALUES (@club, @comp, @opponent, @date, @kickoff, @stadium, @formation, @result, @for, @against)",
                        Db.P("@club", clubIds[club]), Db.P("@comp", competitionId),
                        Db.P("@opponent", opponents[(matchCounter + 2) % opponents.Length]),
                        Db.P("@date", matchDate), Db.P("@kickoff", new TimeSpan(15, 0, 0)),
                        Db.P("@stadium", stadiumIds[club == "WSU BCC" ? "WSU BCC Park" : club == "WSU Dream Team" ? "Dream Arena" : "Jane Furse Stadium"]),
                        Db.P("@formation", completed ? formations[matchCounter % formations.Length] : null),
                        Db.P("@result", result), Db.P("@for", goalFor), Db.P("@against", goalAgainst));

                    if (completed)
                    {
                        clubCompletedMatches[club].Add(matchId);

                        var active = rosters[club].Take(club == "WSU BCC" ? 20 : 12).ToList();
                        var starters = active.Take(11).ToList();
                        var bench = active.Skip(11).Take(5).ToList();

                        foreach (var player in starters)
                        {
                            await ExecAsync(connection, tx,
                                "INSERT INTO dbo.Squads (MatchId, PlayerId, IsStarting) VALUES (@match, @player, 1)",
                                Db.P("@match", matchId), Db.P("@player", player.PlayerId));
                        }

                        foreach (var player in bench)
                        {
                            await ExecAsync(connection, tx,
                                "INSERT INTO dbo.Squads (MatchId, PlayerId, IsStarting) VALUES (@match, @player, 0)",
                                Db.P("@match", matchId), Db.P("@player", player.PlayerId));
                        }

                        var totalGoals = goalFor ?? 0;
                        var totalAssists = Math.Max(0, totalGoals - 1);

                        for (var p = 0; p < starters.Count; p++)
                        {
                            var goalsForPlayer = 0;
                            if (totalGoals > 0 && p == 9) goalsForPlayer = 1;
                            if (totalGoals > 1 && p == 10) goalsForPlayer = totalGoals - 1;

                            var assistsForPlayer = 0;
                            if (totalAssists > 0 && p == 7) assistsForPlayer = 1;
                            if (totalAssists > 1 && p == 8) assistsForPlayer = totalAssists - 1;

                            var yellow = (matchCounter + p) % 11 == 0 ? 1 : 0;
                            var red = matchCounter % 7 == 3 && p == 4 ? 1 : 0;

                            await ExecAsync(connection, tx,
                                @"INSERT INTO dbo.PlayerStats
                                    (MatchId, PlayerId, Goal, Assist, YellowCard, RedCard, MinutesPlayed)
                                  VALUES (@match, @player, @goal, @assist, @yellow, @red, 90)",
                                Db.P("@match", matchId), Db.P("@player", starters[p].PlayerId),
                                Db.P("@goal", goalsForPlayer), Db.P("@assist", assistsForPlayer),
                                Db.P("@yellow", yellow), Db.P("@red", red));
                        }

                        foreach (var player in bench)
                        {
                            await ExecAsync(connection, tx,
                                @"INSERT INTO dbo.PlayerStats
                                    (MatchId, PlayerId, Goal, Assist, YellowCard, RedCard, MinutesPlayed)
                                  VALUES (@match, @player, 0, 0, 0, 0, @minutes)",
                                Db.P("@match", matchId), Db.P("@player", player.PlayerId),
                                Db.P("@minutes", 15 + (matchCounter % 15)));
                        }

                        matchCounter++;
                    }
                    else
                    {
                        clubUpcomingMatches[club].Add(matchId);
                    }
                }
            }

            var injuries = new (string Club, int RosterIndex, string Type, int DaysAgo, string Estimate, string? Actual)[]
            {
                ("WSU BCC", 2, "Hamstring strain", 40, "3 weeks", "Recovered in 19 days"),
                ("WSU BCC", 4, "Ankle sprain", 25, "2 weeks", "Recovered in 12 days"),
                ("WSU BCC", 5, "Knee ligament strain", 10, "6 weeks", null),
                ("WSU BCC", 7, "Calf tightness", 4, "10 days", null),
                ("WSU Dream Team", 3, "Groin strain", 30, "2 weeks", "Recovered in 15 days"),
                ("WSU Dream Team", 6, "Concussion", 6, "7 days", null),
                ("Juniors FC", 2, "Shoulder injury", 35, "3 weeks", "Recovered in 21 days"),
                ("Juniors FC", 8, "Foot bruise", 8, "8 days", null)
            };

            foreach (var (club, rosterIndex, type, daysAgo, estimate, actual) in injuries)
            {
                await ExecAsync(connection, tx,
                    @"INSERT INTO dbo.InjuryHistories (PlayerId, InjuryType, InjuryDate, EstimatedRecovery, ActualRecovery)
                      VALUES (@player, @type, @date, @estimate, @actual)",
                    Db.P("@player", rosters[club][rosterIndex].PlayerId), Db.P("@type", type),
                    Db.P("@date", today.AddDays(-daysAgo)), Db.P("@estimate", estimate), Db.P("@actual", actual));
            }

            foreach (var club in new[] { "WSU BCC", "WSU Dream Team", "Juniors FC" })
            {
                var roster = rosters[club];
                await ExecAsync(connection, tx,
                    @"INSERT INTO dbo.Unavailabilities (PlayerId, StartDate, EndDate, Reason)
                      VALUES (@player, @start, @end, @reason)",
                    Db.P("@player", roster[Math.Min(6, roster.Count - 1)].PlayerId),
                    Db.P("@start", today.AddDays(2)), Db.P("@end", today.AddDays(5)),
                    Db.P("@reason", "Academic exams"));

                await ExecAsync(connection, tx,
                    @"INSERT INTO dbo.Unavailabilities (PlayerId, StartDate, EndDate, Reason)
                      VALUES (@player, @start, @end, @reason)",
                    Db.P("@player", roster[Math.Min(8, roster.Count - 1)].PlayerId),
                    Db.P("@start", today.AddDays(14)), Db.P("@end", today.AddDays(18)),
                    Db.P("@reason", "Family commitments"));

                var titles = new[]
                {
                    ("Matchday squad confirmed", "The coach has named a strong squad for the next fixture. Arrive 60 minutes before kickoff."),
                    ("Training moved to Thursday", "This week's session moves to Thursday at 17:00 on the main pitch. Bring water."),
                    ("Membership fees due", "Please settle outstanding membership fees at the club office before the end of the month.")
                };

                foreach (var (title, description) in titles)
                {
                    await ExecAsync(connection, tx,
                        @"INSERT INTO dbo.Announcements (CoordinatorId, ClubId, Title, Description)
                          VALUES (@coordinator, @club, @title, @description)",
                        Db.P("@coordinator", coordinatorIds[club]), Db.P("@club", clubIds[club]),
                        Db.P("@title", title), Db.P("@description", description));
                }
            }

            var histories = new (string Club, string Title, string Description, int Year)[]
            {
                ("WSU BCC", "First league title", "The senior side lifted the regional league trophy after an unbeaten run in the second half of the season.", 2019),
                ("WSU BCC", "New clubhouse opened", "The club opened a new clubhouse and changing rooms beside the main pitch, funded by members and local sponsors.", 2022),
                ("WSU Dream Team", "Women's side founded", "The club launched its women's team, giving local players a competitive home in the East London league.", 2015),
                ("WSU Dream Team", "Provincial cup final", "The Dream Team reached its first provincial cup final, finishing runners-up after a penalty shootout.", 2023),
                ("Juniors FC", "Academy launched", "A youth academy was started in Jane Furse to develop players from under-12 level upwards.", 2020),
                ("Juniors FC", "First provincial title", "The under-19 side won the provincial championship, the club's first major trophy.", 2024)
            };

            foreach (var (club, title, description, year) in histories)
            {
                await ExecAsync(connection, tx,
                    @"INSERT INTO dbo.TeamHistories (CoordinatorId, ClubId, Title, Description, [Year])
                      VALUES (@coordinator, @club, @title, @description, @year)",
                    Db.P("@coordinator", coordinatorIds[club]), Db.P("@club", clubIds[club]),
                    Db.P("@title", title), Db.P("@description", description), Db.P("@year", year));
            }

            var comments = new[]
            {
                "Great team performance, the midfield ran the show.",
                "Defence looked solid today. Clean sheet next time.",
                "That second goal was worth the ticket price alone.",
                "Proud of the boys. Keep this momentum going.",
                "Tough result but the effort was there from start to finish.",
                "The atmosphere at the stadium was electric today."
            };

            var commentIndex = 0;
            foreach (var club in new[] { "WSU BCC", "WSU Dream Team", "Juniors FC" })
            {
                var fans = fanPersonIds[club];
                var matches = clubCompletedMatches[club];

                for (var i = 0; i < 3; i++)
                {
                    await ExecAsync(connection, tx,
                        "INSERT INTO dbo.Comments (MatchId, PersonId, CommentText) VALUES (@match, @person, @text)",
                        Db.P("@match", matches[i % matches.Count]), Db.P("@person", fans[i % fans.Count]),
                        Db.P("@text", comments[commentIndex++ % comments.Length]));
                }
            }

            var matchTotal = await InsertAsync(connection, tx, "SELECT COUNT(*) FROM dbo.Matches;");

            await tx.CommitAsync();
            _logger.LogInformation("Seed data created: {Persons} accounts, {Clubs} clubs, {Matches} matches.",
                65, clubIds.Count, matchTotal);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static async Task<int> InsertStadiumAsync(
        SqlConnection connection, SqlTransaction tx, string name, int capacity, string surface, int cityId)
    {
        return await InsertAsync(connection, tx,
            @"INSERT INTO dbo.Stadiums (StadiumName, Capacity, Surface, CityId)
              OUTPUT INSERTED.StadiumId VALUES (@name, @capacity, @surface, @city)",
            Db.P("@name", name), Db.P("@capacity", capacity), Db.P("@surface", surface), Db.P("@city", cityId));
    }

    private static async Task<Dictionary<string, int>> InsertLookupAsync(
        SqlConnection connection, SqlTransaction tx, string table, string idColumn, string[] names)
    {
        var ids = new Dictionary<string, int>();
        foreach (var name in names)
        {
            ids[name] = await InsertAsync(connection, tx,
                $"INSERT INTO {table} ({idColumn.Replace("Id", "Name")}) OUTPUT INSERTED.{idColumn} VALUES (@name)",
                Db.P("@name", name));
        }

        return ids;
    }

    private static async Task<int> InsertAsync(
        SqlConnection connection, SqlTransaction tx, string sql, params SqlParameter[] parameters)
    {
        await using var command = new SqlCommand(sql, connection, tx);
        command.Parameters.AddRange(parameters);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task ExecAsync(
        SqlConnection connection, SqlTransaction tx, string sql, params SqlParameter[] parameters)
    {
        await using var command = new SqlCommand(sql, connection, tx);
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }
}
