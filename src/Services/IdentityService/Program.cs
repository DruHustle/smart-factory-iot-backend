using SmartFactory.BuildingBlocks.DashboardAccess;
var builder = WebApplication.CreateBuilder(args);
builder.AddDashboardAccess();
var app = builder.Build();
app.UseDashboardAccess("SELECT id FROM users LIMIT 1");
app.MapGet("/api/auth/profile", (HttpContext context) => Results.Ok(DashboardRuntime.User(context)));
app.MapGet("/api/auth/check-role/{role}", (string role, HttpContext context) =>
    new[] { "viewer", "operator", "engineer", "admin" }.Contains(role)
      ? Results.Ok(new { role, hasRole = DashboardRuntime.User(context).Role == role })
      : Results.BadRequest(new { error = "Unknown role" }));
app.Run();
