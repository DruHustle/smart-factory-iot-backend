using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SmartFactory.Services.NotificationService;
namespace SmartFactory.Tests;
public class ResendDeliveryTests
{
    private static IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
        ["RESEND_API_KEY"]="re_test-only", ["RESEND_FROM"]="Smart Factory <sender@example.com>", ["RESEND_ALLOWED_RECIPIENT_DOMAINS"]="example.com"
    }).Build();
    [Fact] public async Task SendsTextFromConfiguredMailboxAndTracksAcceptedResponse() {
        var handler=new Capture(HttpStatusCode.OK);
        var sender=new ResendMailSender(new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") }, Config());
        await sender.Send("engineer@example.com", "Incident", "Details <untrusted>", 42, default);
        Assert.Equal("https://api.resend.com/emails", handler.Url);
        using var payload=JsonDocument.Parse(handler.Body!);
        Assert.Equal("Details <untrusted>", payload.RootElement.GetProperty("text").GetString());
        Assert.Equal("42", payload.RootElement.GetProperty("headers").GetProperty("X-Smart-Factory-Notification-Id").GetString());
        Assert.Equal("smart-factory-notification/42", handler.IdempotencyKey);
    }
    [Fact] public async Task RejectsThrottledRequestAndUnauthorizedRecipientInsteadOfReportingSuccess() {
        var handler=new Capture(HttpStatusCode.TooManyRequests);
        var sender=new ResendMailSender(new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") }, Config());
        var error=await Assert.ThrowsAsync<HttpRequestException>(()=>sender.Send("engineer@example.com", "Incident", "Details", 42, default));
        Assert.Equal(HttpStatusCode.TooManyRequests,error.StatusCode);
        handler.Url=null;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>sender.Send("attacker@other.com", "Incident", "Details", 42, default));
        Assert.Null(handler.Url);
    }
    private sealed class Capture(HttpStatusCode status) : HttpMessageHandler {
        public string? Url; public string? Body; public string? IdempotencyKey;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
            Url=request.RequestUri!.AbsoluteUri; Body=await request.Content!.ReadAsStringAsync(ct);
            Assert.Equal("Bearer",request.Headers.Authorization!.Scheme);
            IdempotencyKey=request.Headers.GetValues("Idempotency-Key").Single();
            return new(status);
        }
    }
}
