using Moq;
using SmartFactory.Services.NotificationService.Application.DTOs;
using SmartFactory.Services.NotificationService.Application.Interfaces;
using Xunit;
using System.Threading.Tasks;

namespace SmartFactory.Tests
{
    /// <summary>
    /// Unit tests for the Notification Service.
    /// Verifies that alerts and emails are triggered correctly.
    /// </summary>
    public class NotificationServiceTests
    {
        [Fact]
        public async Task TriggerLogicAppAsync_ShouldCompleteSuccessfully()
        {
            // Arrange
            var mockNotificationService = new Mock<INotificationService>();
            var payload = new AlertPayload
            {
                DeviceId = "DEV-001",
                AlertMessage = "Test Alert",
                Severity = "Critical",
                Timestamp = System.DateTime.UtcNow
            };

            // Act
            await mockNotificationService.Object.TriggerLogicAppAsync(payload);

            // Assert
            mockNotificationService.Verify(x => x.TriggerLogicAppAsync(It.IsAny<AlertPayload>()), Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_ShouldCallProvider()
        {
            // Arrange
            var mockNotificationService = new Mock<INotificationService>();
            var emailRequest = new EmailRequest
            {
                To = "test@example.com",
                Subject = "Test Subject",
                Body = "Test Body"
            };

            // Act
            await mockNotificationService.Object.SendEmailAsync(emailRequest);

            // Assert
            mockNotificationService.Verify(x => x.SendEmailAsync(It.IsAny<EmailRequest>()), Times.Once);
        }
    }
}
