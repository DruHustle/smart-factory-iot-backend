using System.Net;
using System.Net.Mail;

namespace SmartFactory.Services.NotificationService;

public sealed class SmtpMailSender : IEmailSender
{
    private readonly string host;
    private readonly int port;
    private readonly string username;
    private readonly string password;
    private readonly MailAddress? sender;
    private readonly bool allowAll;
    private readonly HashSet<string> domains;
    private readonly HashSet<string> recipients;

    public bool Configured => sender is not null && password.Length > 0;
    public string Provider => "Gmail SMTP";

    public SmtpMailSender(IConfiguration configuration)
    {
        host = configuration["SMTP_HOST"] ?? "smtp.gmail.com";
        port = int.TryParse(configuration["SMTP_PORT"], out var configuredPort) ? configuredPort : 587;
        username = configuration["SMTP_USERNAME"] ?? "";
        password = configuration["SMTP_PASSWORD"] ?? "";
        sender = MailAddress.TryCreate(configuration["SMTP_FROM"], out var parsed) ? parsed : null;
        allowAll = string.Equals(configuration["SMTP_ALLOW_ALL_RECIPIENTS"], "true", StringComparison.OrdinalIgnoreCase);
        domains = Split(configuration["SMTP_ALLOWED_RECIPIENT_DOMAINS"]);
        recipients = Split(configuration["SMTP_ALLOWED_RECIPIENTS"]);
        if (password.Length > 0 && (sender is null || username.Length == 0 || host.Length == 0 || port is < 1 or > 65535))
            throw new InvalidOperationException("SMTP host, port, username and sender must be valid when SMTP_PASSWORD is configured.");
        if (Configured && !allowAll && domains.Count == 0 && recipients.Count == 0)
            throw new InvalidOperationException("Configure an SMTP recipient policy when SMTP is enabled.");
    }

    public bool Allows(string? email) => email is not null && MailAddress.TryCreate(email, out var address)
        && address.Address == email && (allowAll || recipients.Contains(address.Address) || domains.Contains(address.Host));

    public async Task Send(string recipient, string title, string body, long notificationId, CancellationToken ct)
    {
        if (!Configured || !Allows(recipient)) throw new InvalidOperationException("SMTP delivery is not configured or the recipient is not allowed.");
        using var message = new MailMessage { From = sender!, Subject = title, Body = body };
        message.To.Add(recipient);
        message.Headers.Add("X-Smart-Factory-Notification-Id", notificationId.ToString());
        using var client = new SmtpClient(host, port) {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };
        await client.SendMailAsync(message, ct);
    }

    private static HashSet<string> Split(string? value) => (value ?? "")
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
