using FootballClubManagement;
using FootballClubManagement.Data;
using FootballClubManagement.Repositories;
using FootballClubManagement.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(45);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "FootballClub.Session";
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Error/AccessDenied";
        options.Cookie.Name = "FootballClub.Auth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<Db>();
builder.Services.AddSingleton<DbSeeder>();

builder.Services.AddScoped<PersonRepository>();
builder.Services.AddScoped<ClubRepository>();
builder.Services.AddScoped<PlayerRepository>();
builder.Services.AddScoped<CoachRepository>();
builder.Services.AddScoped<MatchRepository>();
builder.Services.AddScoped<SquadRepository>();
builder.Services.AddScoped<StatisticsRepository>();
builder.Services.AddScoped<InjuryRepository>();
builder.Services.AddScoped<UnavailabilityRepository>();
builder.Services.AddScoped<AnnouncementRepository>();
builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<TeamHistoryRepository>();builder.Services.AddScoped<CompetitionRepository>();
builder.Services.AddScoped<LocationRepository>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<CoachService>();
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<SquadService>();
builder.Services.AddScoped<StatisticsService>();
builder.Services.AddScoped<InjuryService>();
builder.Services.AddScoped<UnavailabilityService>();
builder.Services.AddScoped<AnnouncementService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<TeamHistoryService>();
builder.Services.AddScoped<CompetitionService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<ClubService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/ServerError");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseExceptionHandler("/Error/ServerError");
}

app.UseStatusCodePagesWithReExecute("/Error/NotFound", "?statusCode={0}");
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database seeding was skipped. Run Database/FootballClubDb.sql in SSMS and check the connection string.");
    }
}

app.Run();
