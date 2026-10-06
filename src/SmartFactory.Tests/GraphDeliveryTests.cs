using System.Net;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using SmartFactory.Services.NotificationService;
namespace SmartFactory.Tests;
public class GraphDeliveryTests
{
    private static IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
        ["GRAPH_TENANT_ID"]="00000000-0000-0000-0000-000000000000", ["GRAPH_CLIENT_ID"]="00000000-0000-0000-0000-000000000001",
        ["GRAPH_CLIENT_SECRET"]="test-only", ["GRAPH_SENDER_USER"]="sender@example.com", ["GRAPH_ALLOWED_RECIPIENT_DOMAINS"]="example.com"
    }).Build();
    [Fact] public async Task SendsTextFromConfiguredMailboxAndTracksAcceptedResponse() {
        var handler=new Capture(HttpStatusCode.Accepted);
        var sender=new GraphMailSender(new HttpClient(handler), Config(), new FakeCredential());
        await sender.Send("engineer@example.com", "Incident", "Details <untrusted>", 42, default);
        Assert.Equal("https://graph.microsoft.com/v1.0/users/sender%40example.com/sendMail", handler.Url);
        using var payload=JsonDocument.Parse(handler.Body!);
        Assert.Equal("Text", payload.RootElement.GetProperty("message").GetProperty("body").GetProperty("contentType").GetString());
        Assert.Equal("42", payload.RootElement.GetProperty("message").GetProperty("internetMessageHeaders")[0].GetProperty("value").GetString());
    }
    [Fact] public async Task RejectsThrottledRequestAndUnauthorizedRecipientInsteadOfReportingSuccess() {
        var handler=new Capture(HttpStatusCode.TooManyRequests);
        var sender=new GraphMailSender(new HttpClient(handler), Config(), new FakeCredential());
        var error=await Assert.ThrowsAsync<HttpRequestException>(()=>sender.Send("engineer@example.com", "Incident", "Details", 42, default));
        Assert.Equal(HttpStatusCode.TooManyRequests,error.StatusCode);
        handler.Url=null;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>sender.Send("attacker@other.com", "Incident", "Details", 42, default));
        Assert.Null(handler.Url);
    }
    private sealed class FakeCredential : TokenCredential {
        public override AccessToken GetToken(TokenRequestContext context, CancellationToken ct) => new("test-only-token", DateTimeOffset.UtcNow.AddMinutes(5));
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext context, CancellationToken ct) => ValueTask.FromResult(GetToken(context,ct));
    }
    private sealed class Capture(HttpStatusCode status) : HttpMessageHandler {
        public string? Url; public string? Body;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
            Url=request.RequestUri!.AbsoluteUri; Body=await request.Content!.ReadAsStringAsync(ct);
            Assert.Equal("Bearer",request.Headers.Authorization!.Scheme);
            return new(status);
        }
    }
}
