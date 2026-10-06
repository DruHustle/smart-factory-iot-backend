using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using Azure.Core;
using Azure.Identity;

namespace SmartFactory.Services.NotificationService;
public sealed class GraphMailSender
{
    private readonly HttpClient client;
    private readonly TokenCredential? credential;
    private readonly string sender;
    private readonly HashSet<string> domains;
    public bool Configured => credential is not null;
    public GraphMailSender(HttpClient client, IConfiguration configuration, TokenCredential? testCredential = null)
    {
        this.client = client;
        sender = configuration["GRAPH_SENDER_USER"] ?? "";
        domains = (configuration["GRAPH_ALLOWED_RECIPIENT_DOMAINS"] ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tenant = configuration["GRAPH_TENANT_ID"];
        var id = configuration["GRAPH_CLIENT_ID"];
        var secret = configuration["GRAPH_CLIENT_SECRET"];
        var values = new[] { sender, tenant ?? "", id ?? "", secret ?? "" };
        if (values.Any(value => value.Length > 0) && (!values.All(value => value.Length > 0) || domains.Count == 0))
            throw new InvalidOperationException("Configure all Graph credentials, sender and allowed recipient domains, or leave Graph entirely disabled.");
        if (values.All(value => value.Length > 0)) credential = testCredential ?? new ClientSecretCredential(tenant, id, secret);
    }
    public bool Allows(string? email) => email is not null && MailAddress.TryCreate(email, out var address)
        && address.Address == email && domains.Contains(address.Host);

    public async Task Send(string recipient, string title, string body, long notificationId, CancellationToken ct)
    {
        if (!Configured || !Allows(recipient)) throw new InvalidOperationException("Email delivery is not configured or recipient domain is not allowed.");
        var access = await credential!.GetTokenAsync(new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }), ct);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/users/" + Uri.EscapeDataString(sender) + "/sendMail");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access.Token);
        request.Content = JsonContent.Create(new { message = new {
            subject = title,
            body = new { contentType = "Text", content = body },
            toRecipients = new[] { new { emailAddress = new { address = recipient } } },
            internetMessageHeaders = new[] { new { name = "x-smart-factory-notification-id", value = notificationId.ToString() } }
        }, saveToSentItems = true });
        using var response = await client.SendAsync(request, ct);
        // Accepted means queued by Graph, not delivered to a mailbox.
        if (response.StatusCode != HttpStatusCode.Accepted)
            throw new HttpRequestException("Graph request rejected (HTTP " + (int)response.StatusCode + ").", null, response.StatusCode);
    }
}
