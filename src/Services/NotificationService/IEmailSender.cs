namespace SmartFactory.Services.NotificationService;

public interface IEmailSender
{
    bool Configured { get; }
    string Provider { get; }
    bool Allows(string? email);
    Task Send(string recipient, string title, string body, long notificationId, CancellationToken ct);
}
