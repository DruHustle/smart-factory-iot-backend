using System.Net.Mail;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;

namespace SmartFactory.Services.NotificationService;

public sealed class SesMailSender : IEmailSender
{
    private readonly IAmazonSimpleEmailServiceV2 client;
    private readonly string sender;
    private readonly HashSet<string> domains;
    private readonly HashSet<string> recipients;
    private readonly bool allowAll;

    public bool Configured { get; }
    public string Provider => "Amazon SES";

    public SesMailSender(IAmazonSimpleEmailServiceV2 client, IConfiguration configuration)
    {
        this.client = client;
        sender = configuration["SES_FROM"] ?? "";
        Configured = string.Equals(configuration["SES_ENABLED"], "true", StringComparison.OrdinalIgnoreCase);
        allowAll = string.Equals(configuration["SES_ALLOW_ALL_RECIPIENTS"], "true", StringComparison.OrdinalIgnoreCase);
        domains = Split(configuration["SES_ALLOWED_RECIPIENT_DOMAINS"]);
        recipients = Split(configuration["SES_ALLOWED_RECIPIENTS"]);

        if (Configured && (!MailAddress.TryCreate(sender, out _) || (!allowAll && domains.Count == 0 && recipients.Count == 0)))
            throw new InvalidOperationException("SES_FROM and an explicit SES recipient policy are required when SES is enabled.");
    }

    private static HashSet<string> Split(string? value) => (value ?? "")
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public bool Allows(string? email)
    {
        if (email is null || !MailAddress.TryCreate(email, out var address) || address.Address != email) return false;
        return allowAll || recipients.Contains(address.Address) || domains.Contains(address.Host);
    }

    public async Task Send(string recipient, string title, string body, long notificationId, CancellationToken ct)
    {
        if (!Configured || !Allows(recipient))
            throw new InvalidOperationException("SES delivery is not configured or the recipient is not allowed.");
        await client.SendEmailAsync(new SendEmailRequest {
            FromEmailAddress = sender,
            Destination = new Destination { ToAddresses = [recipient] },
            Content = new EmailContent { Simple = new Message {
                Subject = new Content { Data = title, Charset = "UTF-8" },
                Body = new Body { Text = new Content { Data = body, Charset = "UTF-8" } },
                Headers = [new MessageHeader { Name = "X-Smart-Factory-Notification-Id", Value = notificationId.ToString() }],
            } },
        }, ct);
    }
}
