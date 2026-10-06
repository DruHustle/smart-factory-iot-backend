using Microsoft.Extensions.Configuration;
using SmartFactory.BuildingBlocks.DashboardAccess;
using SmartFactory.Services.AnalyticsService;
using SmartFactory.Services.NotificationService;
namespace SmartFactory.Tests;
public class DashboardServiceTests
{
    [Fact] public void InternalTokenRejectsMissingAndWrongCredentials() {
        var token = new string('x', 32);
        Assert.True(DashboardRuntime.ValidToken(token, token));
        Assert.False(DashboardRuntime.ValidToken("", token));
        Assert.False(DashboardRuntime.ValidToken(new string('y',32), token));
    }
    [Theory] [InlineData(0)] [InlineData(94)] public void AnalyticsRejectsInvalidWindows(int days) {
        Assert.False(CoverageAnalytics.Valid(new(new[]{"urn:test:asset"}, 0, days * 86400000L)));
    }
    [Fact] public void AnalyticsBoundsScopeAndPreservesValidWindow() {
        Assert.True(CoverageAnalytics.Valid(new(new[]{"urn:test:asset"}, 0, 86400000L)));
        Assert.False(CoverageAnalytics.Valid(new(Array.Empty<string>(), 0, 1000)));
        Assert.False(CoverageAnalytics.Valid(new(Enumerable.Repeat("a", 201).ToArray(), 0, 1000)));
    }
    [Fact] public void PartiallyConfiguredResendFailsClosed() {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["RESEND_FROM"] = "sender@example.com" }).Build();
        Assert.Throws<InvalidOperationException>(() => new ResendMailSender(new HttpClient(), config));
    }
    [Fact] public void ResendOnlyAcceptsExplicitRecipientDomains() {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["RESEND_API_KEY"]="re_test", ["RESEND_FROM"]="sender@example.com", ["RESEND_ALLOWED_RECIPIENT_DOMAINS"]="example.com"
        }).Build();
        var sender = new ResendMailSender(new HttpClient(), config);
        Assert.True(sender.Allows("engineer@example.com"));
        Assert.False(sender.Allows("engineer@attacker.com"));
        Assert.False(sender.Allows("Display Name <engineer@example.com>"));
    }
}
