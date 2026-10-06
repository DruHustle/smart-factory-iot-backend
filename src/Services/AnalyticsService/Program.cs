using SmartFactory.BuildingBlocks.DashboardAccess;
using SmartFactory.Services.AnalyticsService;
var builder = WebApplication.CreateBuilder(args);
builder.AddDashboardAccess();
builder.Services.AddSingleton<CoverageAnalytics>();
var app = builder.Build();
app.UseDashboardAccess("SELECT id FROM sensor_readings LIMIT 1");
app.MapPost("/api/analytics/coverage", async (CoverageRequest request, CoverageAnalytics analytics, CancellationToken ct) => {
    if (!CoverageAnalytics.Valid(request)) return Results.BadRequest(new { error = "Select 1–200 assets and a valid period of at most 93 days." });
    return Results.Ok(await analytics.Query(request, ct));
});
app.Run();
