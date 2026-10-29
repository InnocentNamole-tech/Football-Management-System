/* =====================================================================
   Football Club Management System - database schema
   Run this whole script once in SQL Server Management Studio.
   It is safe to run again: it recreates the schema (data is reseeded
   by the web application on first start when Persons is empty).
   ===================================================================== */

IF DB_ID('FootballClubDb') IS NULL
    CREATE DATABASE FootballClubDb;
GO

USE FootballClubDb;
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

/* ---------- drop in dependency order (views first) ---------- */
IF OBJECT_ID('dbo.SquadLineups', 'V') IS NOT NULL DROP VIEW dbo.SquadLineups;
IF OBJECT_ID('dbo.CompetitionEntries', 'V') IS NOT NULL DROP VIEW dbo.CompetitionEntries;
IF OBJECT_ID('dbo.PlayerTotals', 'V') IS NOT NULL DROP VIEW dbo.PlayerTotals;
IF OBJECT_ID('dbo.PlayerOverviews', 'V') IS NOT NULL DROP VIEW dbo.PlayerOverviews;
GO

IF OBJECT_ID('dbo.Comments', 'U') IS NOT NULL DROP TABLE dbo.Comments;
IF OBJECT_ID('dbo.Announcements', 'U') IS NOT NULL DROP TABLE dbo.Announcements;
IF OBJECT_ID('dbo.TeamHistories', 'U') IS NOT NULL DROP TABLE dbo.TeamHistories;
IF OBJECT_ID('dbo.Unavailabilities', 'U') IS NOT NULL DROP TABLE dbo.Unavailabilities;
IF OBJECT_ID('dbo.InjuryHistories', 'U') IS NOT NULL DROP TABLE dbo.InjuryHistories;
IF OBJECT_ID('dbo.PlayerStats', 'U') IS NOT NULL DROP TABLE dbo.PlayerStats;
IF OBJECT_ID('dbo.Squads', 'U') IS NOT NULL DROP TABLE dbo.Squads;
IF OBJECT_ID('dbo.Matches', 'U') IS NOT NULL DROP TABLE dbo.Matches;
IF OBJECT_ID('dbo.CompetitionClubs', 'U') IS NOT NULL DROP TABLE dbo.CompetitionClubs;
IF OBJECT_ID('dbo.Competitions', 'U') IS NOT NULL DROP TABLE dbo.Competitions;
IF OBJECT_ID('dbo.FanClubs', 'U') IS NOT NULL DROP TABLE dbo.FanClubs;
IF OBJECT_ID('dbo.CoachClubs', 'U') IS NOT NULL DROP TABLE dbo.CoachClubs;
IF OBJECT_ID('dbo.PlayerClubs', 'U') IS NOT NULL DROP TABLE dbo.PlayerClubs;
IF OBJECT_ID('dbo.Fans', 'U') IS NOT NULL DROP TABLE dbo.Fans;
IF OBJECT_ID('dbo.Players', 'U') IS NOT NULL DROP TABLE dbo.Players;
IF OBJECT_ID('dbo.Coaches', 'U') IS NOT NULL DROP TABLE dbo.Coaches;
IF OBJECT_ID('dbo.Coordinators', 'U') IS NOT NULL DROP TABLE dbo.Coordinators;
IF OBJECT_ID('dbo.Organisers', 'U') IS NOT NULL DROP TABLE dbo.Organisers;
IF OBJECT_ID('dbo.Admins', 'U') IS NOT NULL DROP TABLE dbo.Admins;
IF OBJECT_ID('dbo.Persons', 'U') IS NOT NULL DROP TABLE dbo.Persons;
IF OBJECT_ID('dbo.PlayerStyles', 'U') IS NOT NULL DROP TABLE dbo.PlayerStyles;
IF OBJECT_ID('dbo.Specializations', 'U') IS NOT NULL DROP TABLE dbo.Specializations;
IF OBJECT_ID('dbo.Positions', 'U') IS NOT NULL DROP TABLE dbo.Positions;
IF OBJECT_ID('dbo.FootballClubs', 'U') IS NOT NULL DROP TABLE dbo.FootballClubs;
IF OBJECT_ID('dbo.Stadiums', 'U') IS NOT NULL DROP TABLE dbo.Stadiums;
IF OBJECT_ID('dbo.Cities', 'U') IS NOT NULL DROP TABLE dbo.Cities;
IF OBJECT_ID('dbo.Provinces', 'U') IS NOT NULL DROP TABLE dbo.Provinces;
GO

/* =====================================================================
   1. LOCATIONS
   ===================================================================== */

CREATE TABLE dbo.Provinces
(
    ProvinceId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Provinces PRIMARY KEY,
    ProvinceName NVARCHAR(50) NOT NULL CONSTRAINT UQ_Provinces_Name UNIQUE
);
GO

CREATE TABLE dbo.Cities
(
    CityId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cities PRIMARY KEY,
    CityName   NVARCHAR(100) NOT NULL,
    ProvinceId INT NOT NULL CONSTRAINT FK_Cities_Provinces REFERENCES dbo.Provinces (ProvinceId),
    CONSTRAINT UQ_Cities_Name_Province UNIQUE (CityName, ProvinceId)
);
GO

CREATE TABLE dbo.Stadiums
(
    StadiumId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Stadiums PRIMARY KEY,
    StadiumName NVARCHAR(100) NOT NULL,
    Capacity    INT NOT NULL CONSTRAINT CK_Stadiums_Capacity CHECK (Capacity > 0),
    Surface     NVARCHAR(30) NOT NULL,
    CityId      INT NOT NULL CONSTRAINT FK_Stadiums_Cities REFERENCES dbo.Cities (CityId),
    CONSTRAINT UQ_Stadiums_Name_City UNIQUE (StadiumName, CityId)
);
GO

CREATE TABLE dbo.FootballClubs
(
    ClubId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FootballClubs PRIMARY KEY,
    ClubName      NVARCHAR(100) NOT NULL CONSTRAINT UQ_FootballClubs_Name UNIQUE,
    FoundedYear   INT NOT NULL CONSTRAINT CK_FootballClubs_FoundedYear CHECK (FoundedYear BETWEEN 1850 AND 2100),
    Gender        NVARCHAR(10) NOT NULL CONSTRAINT CK_FootballClubs_Gender CHECK (Gender IN ('Men','Women','Mixed')),
    AgeGroup      NVARCHAR(10) NOT NULL CONSTRAINT CK_FootballClubs_AgeGroup CHECK (AgeGroup IN ('U13','U15','U17','U19','Senior')),
    CityId        INT NOT NULL CONSTRAINT FK_FootballClubs_Cities REFERENCES dbo.Cities (CityId),
    HomeStadiumId INT NOT NULL CONSTRAINT FK_FootballClubs_Stadiums REFERENCES dbo.Stadiums (StadiumId)
);
GO

/* =====================================================================
   2. LOOKUPS
   ===================================================================== */

CREATE TABLE dbo.Positions
(
    PositionId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Positions PRIMARY KEY,
    PositionName NVARCHAR(30) NOT NULL CONSTRAINT UQ_Positions_Name UNIQUE
);
GO

CREATE TABLE dbo.Specializations
(
    SpecializationId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Specializations PRIMARY KEY,
    SpecializationName NVARCHAR(50) NOT NULL CONSTRAINT UQ_Specializations_Name UNIQUE
);
GO

CREATE TABLE dbo.PlayerStyles
(
    PlayerStyleId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlayerStyles PRIMARY KEY,
    PlayerStyleName NVARCHAR(50) NOT NULL CONSTRAINT UQ_PlayerStyles_Name UNIQUE
);
GO

/* =====================================================================
   3. USERS
   ===================================================================== */

CREATE TABLE dbo.Persons
(
    PersonId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Persons PRIMARY KEY,
    FirstName        NVARCHAR(50) NOT NULL,
    LastName         NVARCHAR(50) NOT NULL,
    Email            NVARCHAR(100) NOT NULL CONSTRAINT UQ_Persons_Email UNIQUE,
    Username         NVARCHAR(20) NOT NULL CONSTRAINT UQ_Persons_Username UNIQUE,
    Phone            VARCHAR(10) NOT NULL,
    PasswordHash     VARCHAR(256) NOT NULL,
    ProfilePhotoPath VARCHAR(255) NULL,
    FailedLoginCount INT NOT NULL CONSTRAINT DF_Persons_FailedLoginCount DEFAULT 0,
    LockoutEnd       DATETIME2(0) NULL,
    CreatedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_Persons_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Persons_Phone CHECK (LEN(Phone) = 10 AND Phone LIKE '0%' AND Phone NOT LIKE '%[^0-9]%'),
    CONSTRAINT CK_Persons_FirstName CHECK (FirstName NOT LIKE '%[^A-Za-z ]%' AND LEN(FirstName) BETWEEN 2 AND 50),
    CONSTRAINT CK_Persons_LastName CHECK (LastName NOT LIKE '%[^A-Za-z ]%' AND LEN(LastName) BETWEEN 2 AND 50)
);
GO

CREATE TABLE dbo.Admins
(
    AdminId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Admins PRIMARY KEY,
    PersonId INT NOT NULL CONSTRAINT UQ_Admins_Person UNIQUE
                          CONSTRAINT FK_Admins_Persons REFERENCES dbo.Persons (PersonId)
);
GO

CREATE TABLE dbo.Organisers
(
    OrganiserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Organisers PRIMARY KEY,
    PersonId    INT NOT NULL CONSTRAINT UQ_Organisers_Person UNIQUE
                            CONSTRAINT FK_Organisers_Persons REFERENCES dbo.Persons (PersonId)
);
GO

CREATE TABLE dbo.Coordinators
(
    CoordinatorId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Coordinators PRIMARY KEY,
    PersonId      INT NOT NULL CONSTRAINT UQ_Coordinators_Person UNIQUE
                              CONSTRAINT FK_Coordinators_Persons REFERENCES dbo.Persons (PersonId),
    ClubId        INT NOT NULL CONSTRAINT FK_Coordinators_FootballClubs REFERENCES dbo.FootballClubs (ClubId)
);
GO

CREATE TABLE dbo.TeamHistories
(
    HistoryId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeamHistories PRIMARY KEY,
    CoordinatorId INT NOT NULL CONSTRAINT FK_TeamHistories_Coordinators REFERENCES dbo.Coordinators (CoordinatorId),
    ClubId        INT NOT NULL CONSTRAINT FK_TeamHistories_FootballClubs REFERENCES dbo.FootballClubs (ClubId),
    Title         NVARCHAR(120) NOT NULL,
    Description   NVARCHAR(1000) NOT NULL,
    [Year]        INT NOT NULL CONSTRAINT CK_TeamHistories_Year CHECK ([Year] BETWEEN 1850 AND 2100)
);
GO

CREATE INDEX IX_TeamHistories_ClubId ON dbo.TeamHistories (ClubId);
GO

CREATE TABLE dbo.Coaches
(
    CoachId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Coaches PRIMARY KEY,
    PersonId     INT NOT NULL CONSTRAINT UQ_Coaches_Person UNIQUE
                              CONSTRAINT FK_Coaches_Persons REFERENCES dbo.Persons (PersonId),
    LicenseLevel NVARCHAR(10) NOT NULL CONSTRAINT DF_Coaches_LicenseLevel DEFAULT 'SAFA C'
                              CONSTRAINT CK_Coaches_LicenseLevel CHECK (LicenseLevel IN ('SAFA A','SAFA B','SAFA C','SAFA D'))
);
GO

CREATE TABLE dbo.Players
(
    PlayerId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Players PRIMARY KEY,
    PersonId      INT NOT NULL CONSTRAINT UQ_Players_Person UNIQUE
                              CONSTRAINT FK_Players_Persons REFERENCES dbo.Persons (PersonId),
    Nickname      NVARCHAR(30) NOT NULL,
    CityId        INT NOT NULL CONSTRAINT FK_Players_Cities REFERENCES dbo.Cities (CityId),
    PlayerStyleId INT NULL CONSTRAINT FK_Players_PlayerStyles REFERENCES dbo.PlayerStyles (PlayerStyleId)
);
GO

CREATE TABLE dbo.Fans
(
    FanId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Fans PRIMARY KEY,
    PersonId INT NOT NULL CONSTRAINT UQ_Fans_Person UNIQUE
                        CONSTRAINT FK_Fans_Persons REFERENCES dbo.Persons (PersonId)
);
GO

/* =====================================================================
   4. CLUB MEMBERSHIPS
   ===================================================================== */

CREATE TABLE dbo.PlayerClubs
(
    PlayerClubId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlayerClubs PRIMARY KEY,
    PlayerId     INT NOT NULL CONSTRAINT FK_PlayerClubs_Players REFERENCES dbo.Players (PlayerId),
    ClubId       INT NOT NULL CONSTRAINT FK_PlayerClubs_FootballClubs REFERENCES dbo.FootballClubs (ClubId),
    PositionId   INT NOT NULL CONSTRAINT FK_PlayerClubs_Positions REFERENCES dbo.Positions (PositionId),
    JerseyNumber INT NOT NULL CONSTRAINT CK_PlayerClubs_Jersey CHECK (JerseyNumber BETWEEN 1 AND 99),
    DateJoined   DATE NOT NULL,
    IsActive     BIT NOT NULL CONSTRAINT DF_PlayerClubs_IsActive DEFAULT 1
);
GO

CREATE UNIQUE INDEX UX_PlayerClubs_Club_Jersey_Active
    ON dbo.PlayerClubs (ClubId, JerseyNumber)
    WHERE IsActive = 1;
GO

CREATE INDEX IX_PlayerClubs_ClubId ON dbo.PlayerClubs (ClubId);
CREATE INDEX IX_PlayerClubs_PlayerId ON dbo.PlayerClubs (PlayerId);
GO

CREATE TABLE dbo.CoachClubs
(
    CoachClubId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CoachClubs PRIMARY KEY,
    CoachId         INT NOT NULL CONSTRAINT FK_CoachClubs_Coaches REFERENCES dbo.Coaches (CoachId),
    ClubId          INT NOT NULL CONSTRAINT FK_CoachClubs_FootballClubs REFERENCES dbo.FootballClubs (ClubId),
    SpecializationId INT NOT NULL CONSTRAINT FK_CoachClubs_Specializations REFERENCES dbo.Specializations (SpecializationId),
    DateJoined      DATE NOT NULL
);
GO

CREATE TABLE dbo.FanClubs
(
    FanClubId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FanClubs PRIMARY KEY,
    FanId     INT NOT NULL CONSTRAINT FK_FanClubs_Fans REFERENCES dbo.Fans (FanId),
    ClubId    INT NOT NULL CONSTRAINT FK_FanClubs_FootballClubs REFERENCES dbo.FootballClubs (ClubId),
    DateJoined DATE NOT NULL
);
GO

/* =====================================================================
   5. ACTIVITIES
   ===================================================================== */

CREATE TABLE dbo.Competitions
(
    CompetitionId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Competitions PRIMARY KEY,
    CompetitionName  NVARCHAR(100) NOT NULL,
    CompetitionType  NVARCHAR(20) NOT NULL CONSTRAINT CK_Competitions_Type CHECK (CompetitionType IN ('Friendly','League','Cup','Tournament','Exhibition')),
    Gender           NVARCHAR(10) NOT NULL CONSTRAINT CK_Competitions_Gender CHECK (Gender IN ('Men','Women','Mixed')),
    AgeGroup         NVARCHAR(10) NOT NULL CONSTRAINT CK_Competitions_AgeGroup CHECK (AgeGroup IN ('U13','U15','U17','U19','Senior')),
    OrganiserId      INT NOT NULL CONSTRAINT FK_Competitions_Organisers REFERENCES dbo.Organisers (OrganiserId),
    StadiumId        INT NOT NULL CONSTRAINT FK_Competitions_Stadiums REFERENCES dbo.Stadiums (StadiumId),
    StartDate        DATE NOT NULL,
    StartTime        TIME(0) NOT NULL,
    EndDate          DATE NOT NULL,
    TeamCount        INT NOT NULL CONSTRAINT CK_Competitions_TeamCount CHECK (TeamCount >= 2),
    AffiliationFee   DECIMAL(10,2) NOT NULL CONSTRAINT CK_Competitions_AffiliationFee CHECK (AffiliationFee >= 0),
    RewardPrice      INT NOT NULL CONSTRAINT CK_Competitions_RewardPrice CHECK (RewardPrice >= 0),
    IndividualAward  NVARCHAR(3) NOT NULL CONSTRAINT CK_Competitions_IndividualAward CHECK (IndividualAward IN ('Yes','No')),
    TrophyMedal      NVARCHAR(3) NOT NULL CONSTRAINT CK_Competitions_TrophyMedal CHECK (TrophyMedal IN ('Yes','No')),
    Promotion        NVARCHAR(3) NOT NULL CONSTRAINT CK_Competitions_Promotion CHECK (Promotion IN ('Yes','No')),
    KitSet           NVARCHAR(3) NOT NULL CONSTRAINT CK_Competitions_KitSet CHECK (KitSet IN ('Yes','No')),
    CompetitionStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_Competitions_Status DEFAULT 'Upcoming'
                                    CONSTRAINT CK_Competitions_Status CHECK (CompetitionStatus IN ('Upcoming','In Progress','Completed')),
    CONSTRAINT CK_Competitions_Dates CHECK (EndDate >= StartDate),
    CONSTRAINT CK_Competitions_Friendly CHECK
    (
        CompetitionType <> 'Friendly'
        OR (RewardPrice = 0 AND IndividualAward = 'No' AND TrophyMedal = 'No' AND Promotion = 'No' AND KitSet = 'No')
    )
);
GO

CREATE TABLE dbo.CompetitionClubs
(
    CompetitionClubId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CompetitionClubs PRIMARY KEY,
    CompetitionId     INT NOT NULL CONSTRAINT FK_CompetitionClubs_Competitions REFERENCES dbo.Competitions (CompetitionId),
    ClubId            INT NOT NULL CONSTRAINT FK_CompetitionClubs_FootballClubs REFERENCES dbo.FootballClubs (ClubId),
    RequestStatus     NVARCHAR(10) NOT NULL CONSTRAINT DF_CompetitionClubs_Status DEFAULT 'Pending'
                                   CONSTRAINT CK_CompetitionClubs_Status CHECK (RequestStatus IN ('Pending','Approved','Rejected')),
    RequestedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_CompetitionClubs_RequestedAt DEFAULT SYSUTCDATETIME(),
    Outcome           NVARCHAR(20) NULL CONSTRAINT CK_CompetitionClubs_Outcome CHECK (Outcome IN ('In Progress','Won','Lost')),
    StageReached      NVARCHAR(20) NULL CONSTRAINT CK_CompetitionClubs_Stage CHECK (StageReached IN ('Group Stage','Round of 16','Quarter-final','Semi-final','Final')),
    CONSTRAINT UQ_CompetitionClubs UNIQUE (CompetitionId, ClubId),
    CONSTRAINT CK_CompetitionClubs_StageOnlyWithOutcome CHECK
    (
        StageReached IS NULL OR Outcome IN ('In Progress','Lost')
    )
);
GO

CREATE TABLE dbo.Matches
(
    MatchId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Matches PRIMARY KEY,
    ClubId        INT NOT NULL CONSTRAINT FK_Matches_FootballClubs REFERENCES dbo.FootballClubs (ClubId),
    CompetitionId INT NULL CONSTRAINT FK_Matches_Competitions REFERENCES dbo.Competitions (CompetitionId),
    Opponent      NVARCHAR(100) NOT NULL,
    MatchDate     DATE NOT NULL,
    KickoffTime   TIME(0) NOT NULL,
    StadiumId     INT NOT NULL CONSTRAINT FK_Matches_Stadiums REFERENCES dbo.Stadiums (StadiumId),
    Formation     NVARCHAR(20) NULL,
    Result        NVARCHAR(10) NULL CONSTRAINT CK_Matches_Result CHECK (Result IN ('Win','Loss','Draw')),
    GoalFor       INT NULL CONSTRAINT CK_Matches_GoalFor CHECK (GoalFor >= 0),
    GoalAgainst   INT NULL CONSTRAINT CK_Matches_GoalAgainst CHECK (GoalAgainst >= 0),
    CONSTRAINT CK_Matches_ResultConsistency CHECK
    (
        (Result IS NULL AND GoalFor IS NULL AND GoalAgainst IS NULL)
        OR (Result IS NOT NULL AND GoalFor IS NOT NULL AND GoalAgainst IS NOT NULL)
    )
);
GO

CREATE INDEX IX_Matches_ClubId_MatchDate ON dbo.Matches (ClubId, MatchDate);
GO

CREATE TABLE dbo.Squads
(
    SquadId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Squads PRIMARY KEY,
    MatchId    INT NOT NULL CONSTRAINT FK_Squads_Matches REFERENCES dbo.Matches (MatchId),
    PlayerId   INT NOT NULL CONSTRAINT FK_Squads_Players REFERENCES dbo.Players (PlayerId),
    IsStarting BIT NOT NULL CONSTRAINT DF_Squads_IsStarting DEFAULT 0,
    CONSTRAINT UQ_Squads_Match_Player UNIQUE (MatchId, PlayerId)
);
GO

CREATE TABLE dbo.PlayerStats
(
    PlayerStatId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlayerStats PRIMARY KEY,
    MatchId      INT NOT NULL CONSTRAINT FK_PlayerStats_Matches REFERENCES dbo.Matches (MatchId),
    PlayerId     INT NOT NULL CONSTRAINT FK_PlayerStats_Players REFERENCES dbo.Players (PlayerId),
    Goal         INT NOT NULL CONSTRAINT DF_PlayerStats_Goal DEFAULT 0 CONSTRAINT CK_PlayerStats_Goal CHECK (Goal >= 0),
    Assist       INT NOT NULL CONSTRAINT DF_PlayerStats_Assist DEFAULT 0 CONSTRAINT CK_PlayerStats_Assist CHECK (Assist >= 0),
    YellowCard   INT NOT NULL CONSTRAINT DF_PlayerStats_Yellow DEFAULT 0 CONSTRAINT CK_PlayerStats_Yellow CHECK (YellowCard BETWEEN 0 AND 2),
    RedCard      INT NOT NULL CONSTRAINT DF_PlayerStats_Red DEFAULT 0 CONSTRAINT CK_PlayerStats_Red CHECK (RedCard BETWEEN 0 AND 1),
    MinutesPlayed INT NOT NULL CONSTRAINT DF_PlayerStats_Minutes DEFAULT 0 CONSTRAINT CK_PlayerStats_Minutes CHECK (MinutesPlayed BETWEEN 0 AND 130),
    CONSTRAINT UQ_PlayerStats_Match_Player UNIQUE (MatchId, PlayerId)
);
GO

CREATE INDEX IX_PlayerStats_PlayerId ON dbo.PlayerStats (PlayerId);
GO

CREATE TABLE dbo.InjuryHistories
(
    InjuryHistoryId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InjuryHistories PRIMARY KEY,
    PlayerId          INT NOT NULL CONSTRAINT FK_InjuryHistories_Players REFERENCES dbo.Players (PlayerId),
    InjuryType        NVARCHAR(100) NOT NULL,
    InjuryDate        DATE NOT NULL,
    EstimatedRecovery VARCHAR(50) NOT NULL,
    ActualRecovery    VARCHAR(50) NULL
);
GO

CREATE INDEX IX_InjuryHistories_PlayerId ON dbo.InjuryHistories (PlayerId);
GO

CREATE TABLE dbo.Unavailabilities
(
    UnavailabilityId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Unavailabilities PRIMARY KEY,
    PlayerId         INT NOT NULL CONSTRAINT FK_Unavailabilities_Players REFERENCES dbo.Players (PlayerId),
    StartDate        DATE NOT NULL,
    EndDate          DATE NOT NULL,
    Reason           NVARCHAR(200) NOT NULL,
    CONSTRAINT CK_Unavailabilities_Dates CHECK (EndDate >= StartDate)
);
GO

CREATE TABLE dbo.Announcements
(
    AnnouncementId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Announcements PRIMARY KEY,
    CoordinatorId  INT NOT NULL CONSTRAINT FK_Announcements_Coordinators REFERENCES dbo.Coordinators (CoordinatorId),
    ClubId         INT NOT NULL CONSTRAINT FK_Announcements_FootballClubs REFERENCES dbo.FootballClubs (ClubId),
    Title          NVARCHAR(120) NOT NULL,
    Description    NVARCHAR(1000) NOT NULL,
    DatePosted     DATETIME2(0) NOT NULL CONSTRAINT DF_Announcements_DatePosted DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_Announcements_ClubId ON dbo.Announcements (ClubId);
GO

CREATE TABLE dbo.Comments
(
    CommentId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Comments PRIMARY KEY,
    MatchId     INT NOT NULL CONSTRAINT FK_Comments_Matches REFERENCES dbo.Matches (MatchId),
    PersonId    INT NOT NULL CONSTRAINT FK_Comments_Persons REFERENCES dbo.Persons (PersonId),
    CommentText VARCHAR(500) NOT NULL,
    PostedAt    DATETIME2(0) NOT NULL CONSTRAINT DF_Comments_PostedAt DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_Comments_MatchId ON dbo.Comments (MatchId);
GO

/* =====================================================================
   6. VIEWS
   ===================================================================== */

CREATE VIEW dbo.PlayerOverviews
AS
SELECT  p.PlayerId,
        p.PersonId,
        pe.FirstName,
        pe.LastName,
        pe.Email,
        pe.Username,
        pe.Phone,
        pe.ProfilePhotoPath,
        p.Nickname,
        pc.PlayerClubId,
        pc.ClubId,
        c.ClubName,
        pc.PositionId,
        pos.PositionName,
        pc.JerseyNumber,
        pc.IsActive,
        pc.DateJoined,
        ps.PlayerStyleId,
        ps.PlayerStyleName,
        p.CityId,
        ci.CityName,
        pr.ProvinceName
FROM dbo.Players p
JOIN dbo.Persons pe ON pe.PersonId = p.PersonId
JOIN dbo.PlayerClubs pc ON pc.PlayerId = p.PlayerId
JOIN dbo.FootballClubs c ON c.ClubId = pc.ClubId
JOIN dbo.Positions pos ON pos.PositionId = pc.PositionId
LEFT JOIN dbo.PlayerStyles ps ON ps.PlayerStyleId = p.PlayerStyleId
JOIN dbo.Cities ci ON ci.CityId = p.CityId
JOIN dbo.Provinces pr ON pr.ProvinceId = ci.ProvinceId;
GO

CREATE VIEW dbo.PlayerTotals
AS
SELECT  s.PlayerId,
        COUNT(*) AS MatchesPlayed,
        SUM(s.Goal) AS Goals,
        SUM(s.Assist) AS Assists,
        SUM(s.YellowCard) AS YellowCards,
        SUM(s.RedCard) AS RedCards,
        SUM(s.MinutesPlayed) AS MinutesPlayed
FROM dbo.PlayerStats s
GROUP BY s.PlayerId;
GO

CREATE VIEW dbo.CompetitionEntries
AS
SELECT  cc.CompetitionClubId,
        cc.CompetitionId,
        co.CompetitionName,
        co.CompetitionType,
        co.Gender,
        co.AgeGroup,
        co.StartDate,
        co.StartTime,
        co.EndDate,
        co.TeamCount,
        co.AffiliationFee,
        co.RewardPrice,
        co.IndividualAward,
        co.TrophyMedal,
        co.Promotion,
        co.KitSet,
        co.CompetitionStatus,
        cc.ClubId,
        cl.ClubName,
        cc.RequestStatus,
        cc.RequestedAt,
        cc.Outcome,
        cc.StageReached,
        co.OrganiserId,
        op.FirstName + ' ' + op.LastName AS OrganiserName,
        op.Email AS OrganiserEmail,
        co.StadiumId,
        st.StadiumName,
        ci.CityName
FROM dbo.CompetitionClubs cc
JOIN dbo.Competitions co ON co.CompetitionId = cc.CompetitionId
JOIN dbo.FootballClubs cl ON cl.ClubId = cc.ClubId
JOIN dbo.Organisers o ON o.OrganiserId = co.OrganiserId
JOIN dbo.Persons op ON op.PersonId = o.PersonId
JOIN dbo.Stadiums st ON st.StadiumId = co.StadiumId
JOIN dbo.Cities ci ON ci.CityId = st.CityId;
GO

CREATE VIEW dbo.SquadLineups
AS
SELECT  sq.SquadId,
        sq.MatchId,
        sq.IsStarting,
        sq.PlayerId,
        pe.FirstName,
        pe.LastName,
        pc.JerseyNumber,
        pos.PositionName,
        m.ClubId,
        m.MatchDate,
        m.Opponent,
        m.Formation
FROM dbo.Squads sq
JOIN dbo.Matches m ON m.MatchId = sq.MatchId
JOIN dbo.Players p ON p.PlayerId = sq.PlayerId
JOIN dbo.Persons pe ON pe.PersonId = p.PersonId
JOIN dbo.PlayerClubs pc ON pc.PlayerId = sq.PlayerId AND pc.ClubId = m.ClubId AND pc.IsActive = 1
JOIN dbo.Positions pos ON pos.PositionId = pc.PositionId;
GO

PRINT 'FootballClubDb schema created.';
GO
