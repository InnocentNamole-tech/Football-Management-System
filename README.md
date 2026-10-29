# Football Club Management System

A role-based football club administration site built with **ASP.NET Core MVC (net10.0)**, **C#**, **Razor** and **SQL Server**. The UI is plain HTML and hand-written CSS — no JavaScript, jQuery, Bootstrap or front-end frameworks. All interactivity is HTML forms plus server-side C#.

## Project layout

```
FootballClubManagement/
├── Controllers/        MVC controllers (thin, role-guarded)
├── Services/           Business rules and authorisation checks
├── Repositories/       Parameterised SQL Server access
├── ViewModels/         Form and display models with DataAnnotations
├── Models/             Domain records and shared constants
├── Views/              Razor views and shared layout
├── Database/           FootballClubDb.sql (schema, views, constraints)
├── DbSeeder.cs         First-run demo data
├── Program.cs          DI, authentication, session, pipeline
└── wwwroot/css/site.css
```

## Prerequisites

- .NET 10 SDK
- SQL Server (Express, Developer or LocalDB-enabled instance)
- SQL Server Management Studio (or `sqlcmd`)

## Setup

1. **Create the database.** Open `Database/FootballClubDb.sql` in SSMS and execute the whole script once. It creates `FootballClubDb`, all tables, constraints, the four reporting views and the filtered unique index on active jerseys. The script is idempotent: running it again recreates the schema.
2. **Check the connection string** in `appsettings.json`:

   ```
   Server=localhost\SQLEXPRESS;Database=FootballClubDb;Trusted_Connection=True;TrustServerCertificate=True
   ```

   This is the string required by the brief. If your SQL Server instance has a different name, update `Server` only.
3. **Run the app:**

   ```
   dotnet run
   ```

   On first start the seeder detects an empty `Persons` table and inserts the demo data: 65 accounts, 3 clubs, 7 competitions, 36 matches, squads, statistics, injuries, unavailabilities, announcements, club histories and comments. All passwords are hashed with `PasswordHasher<Person>` before insert; no plain-text password is stored.

## How it works

- **Public site and self-registration:** the landing page has **Log in** and **Sign up** in the top-right. Only fans and organisers can register themselves: the visitor first chooses Club fan or Competition organiser, then supplies their own details and password. Admin, coordinator, coach and player accounts are issued by the system administrator or the club.
- **Authentication:** cookie authentication with ASP.NET Core `PasswordHasher<Person>`. There is no ASP.NET Identity schema. Successful logins resolve exactly one role (Admin → Organiser → Coordinator → Coach → Player → Fan) and issue claims for `PersonId`, `Username`, `FirstName`, `Role` and `ClubId`.
- **Lockout:** five failed attempts lock the account for five minutes. The message is identical for an unknown username and a wrong password. An administrator can unlock the account from **Accounts**.
- **Forgot password:** a visitor enters the email and username on the account. A non-match shows *No match found for this user name.*; a match opens the new-password page and then returns to sign-in.
- **Authorisation:** role attributes on every controller plus a club-scoped re-check in services. Every id arriving from a URL is validated against the signed-in user's club or person before data is read or written.
- **Club and location administration:** the system administrator registers clubs (name, founded year, gender and age group, home city and matching stadium). Coordinators maintain cities and stadiums with add, edit and view pages. Admin accounts can be removed from **Accounts** with confirmation; the current admin and the last administrator cannot be deleted, and organisers who still own competitions are blocked.
- **Squad access:** only a coach chooses the formation and names the squad through the four-step wizard. Coordinators, players and fans can view a selected squad read-only, including upcoming matches, and see an empty state when the coach has not selected one. Match creation never asks for a formation.
- **Club overview:** players, coaches, coordinators and fans get **My club** with cards for total players, matches played and wins, plus club name, founded year, home stadium, city, age group, gender group, coach and coordinator. A coach profile also shows their SAFA licence level.
- **Club page visibility:** only the system administrator can browse the full list of clubs. On the full club page, club-scoped roles (coordinator, coach, player, fan) are limited to their own club and get **Back to my club** instead of **Back to clubs**; any other club id in the URL is refused with the access-denied page. Organisers and the administrator can review club profiles.
- **Club history:** the coordinator maintains **Team history** entries (title, year, description) with add, edit and confirmation delete. Fans, players and coaches see the same page read-only for their own club.
- **Account self-service:** every signed-in user can change their own password from **Edit profile** by entering the current password and a new one twice; the current password is verified with `PasswordHasher<Person>` before the new hash is stored, and the password is never echoed back to the page.
- **Own-content deletion:** a coordinator can delete announcements they posted, fans can delete their own comments, and players can delete their own unavailability records. Every delete goes through a confirmation page and re-checks ownership and club in the service layer.
- **Data access:** `Microsoft.Data.SqlClient` with parameterised SQL only. Writes that span tables use transactions. Friendly competitions are forced to zero rewards. Unique-index and foreign-key violations are mapped to friendly validation messages.
- **Squad wizard:** a four-step server-side wizard (match & formation → eleven starters → up to eight substitutes → review). The draft lives in session state; the final save re-validates 11 starters, no overlap with the bench, and active own-club players.
- **Validation:** DataAnnotations on view models plus HTML5 attributes in Razor. Server-side validation is always authoritative, even when client-side attributes are bypassed. Photos are limited to JPG/PNG, 2 MB, checked by extension and content type, and stored under `wwwroot/uploads` with a generated name.
- **Posts:** anti-forgery validation is enforced globally, success and error feedback uses TempData with post-redirect-get, and delete/deactivate flows have confirmation pages. Fans write match comments; coordinators, coaches and players can read the comments posted on their own club's matches, but only the fan who wrote a comment can edit or delete it.
- **Navigation:** a CSS-only side drawer (no JavaScript) slides in from the left when the three-line button in the header is selected. The menu is grouped by role and the current page is highlighted. The footer is intentionally minimal.

## Demo accounts

Club staff usernames use the person's first name and club (`karim_bcc`); club emails use the first initial and surname (`KJamal@bcc.ac.za`).

| Role | Username | Password |
| --- | --- | --- |
| Admin | `sysadmin` | `systemAdmin123!` |
| Organiser | `organiser1` | `Organiser123!` |
| Organiser | `organiser2` | `Organiser123!` |
| Coordinator (WSU BCC) | `naledi_bcc` | `Coordinator123!` |
| Coordinator (WSU Dream Team) | `ayanda_dream` | `Coordinator123!` |
| Coordinator (Juniors FC Under 19) | `sibusiso_juniors` | `Coordinator123!` |
| Coach (WSU BCC) | `coach_bcc` | `Coach123!` |
| Coach (WSU BCC) | `coach_bcc2` | `Coach123!` |
| Coach (WSU Dream Team) | `coach_dream` | `Coach123!` |
| Coach (Juniors FC Under 19) | `coach_juniors` | `Coach123!` |
| Player (WSU BCC, captain #9) | `karim_bcc` | `Player123!` |
| Player (WSU BCC) | `sipho_bcc`, `kagiso_bcc` … | `Player123!` |
| Player (WSU Dream Team) | `ayanda_dream2`, `bongani_dream` … | `Player123!` |
| Player (Juniors FC Under 19) | `ayanda_juniors`, `kabelo_juniors` … | `Player123!` |
| Fan (WSU BCC) | `palesa_bcc`, `kabelo_bcc`, `neo_bcc` | `clubFan123!` |
| Fan (WSU Dream Team) | `karabo_dream`, `amogelang_dream`, `refilwe_dream` | `clubFan123!` |
| Fan (Juniors FC Under 19) | `omphile_juniors`, `keabetswe_juniors`, `oarabile_juniors` | `clubFan123!` |

Seed-data highlights: the three clubs are **WSU BCC** (Senior Men, East London, Eastern Cape), **WSU Dream Team** (Senior Women, East London) and **Juniors FC Under 19** (U19 Men, Jane Furse, Limpopo). WSU BCC has 22 players including two inactive, the other two clubs have 12 each; competitions cover Friendly, League, Cup and Tournament across Men/Women/Mixed and U13–Senior with Pending/Approved/Rejected requests and Won/Lost/In Progress outcomes; matches include wins, losses and draws with named squads for completed games. Each club also has two seeded history entries written by its coordinator.

## Resetting the demo

Re-run `Database/FootballClubDb.sql` in SSMS, then start the app again. The seeder repopulates everything from scratch.
