using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;

namespace SmartFactory.Services.NotificationService;

public sealed class ResendMailSender : IEmailSender
{
    private readonly HttpClient client;
    private readonly string apiKey;
    private readonly string sender;
    private readonly HashSet<string> domains;

    public bool Configured => apiKey.Length > 0;
    public string Provider => "Resend";

    public ResendMailSender(HttpClient client, IConfiguration configuration)
    {
        this.client = client;
        apiKey = configuration["RESEND_API_KEY"] ?? "";
        sender = configuration["RESEND_FROM"] ?? "";
        domains = (configuration["RESEND_ALLOWED_RECIPIENT_DOMAINS"] ?? "")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var values = new[] { apiKey, sender };
        if (values.Any(value => value.Length > 0) && (!values.All(value => value.Length > 0) || domains.Count == 0))
            throw new InvalidOperationException("Configure the Resend API key, sender and allowed recipient domains, or leave Resend entirely disabled.");
        if (Configured && (!apiKey.StartsWith("re_", StringComparison.Ordinal) || !MailAddress.TryCreate(sender, out _)))
            throw new InvalidOperationException("RESEND_API_KEY or RESEND_FROM is invalid.");
    }

    public bool Allows(string? email) => email is not null && MailAddress.TryCreate(email, out var address)
        && address.Address == email && domains.Contains(address.Host);

    public async Task Send(string recipient, string title, string body, long notificationId, CancellationToken ct)
    {
        if (!Configured || !Allows(recipient)) throw new InvalidOperationException("Email delivery is not configured or recipient domain is not allowed.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Add("Idempotency-Key", $"smart-factory-notification/{notificationId}");
        request.Content = JsonContent.Create(new {
            from = sender,
            to = new[] { recipient },
            subject = title,
            text = body,
            headers = new Dictionary<string, string> { ["X-Smart-Factory-Notification-Id"] = notificationId.ToString() },
        });
        using var response = await client.SendAsync(request, ct);
        if (response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.Created))
            throw new HttpRequestException("Resend request rejected (HTTP " + (int)response.StatusCode + ").", null, response.StatusCode);
    }
}
