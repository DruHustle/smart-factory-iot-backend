using SmartFactory.BuildingBlocks.DashboardAccess;
using SmartFactory.Services.NotificationService;
var builder = WebApplication.CreateBuilder(args);
builder.AddDashboardAccess();
builder.Services.AddHttpClient("resend", client => {
    client.BaseAddress = new Uri("https://api.resend.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<ResendMailSender>(services => new ResendMailSender(services.GetRequiredService<IHttpClientFactory>().CreateClient("resend"), builder.Configuration));
builder.Services.AddHostedService<NotificationDeliveryWorker>();
var app = builder.Build();
app.UseDashboardAccess("SELECT id FROM notification_inbox LIMIT 1");
app.MapGet("/api/notifications/status", (ResendMailSender sender) => Results.Ok(new { emailConfigured = sender.Configured, provider = "Resend", delivery = "at-least-once" }));
app.Run();
