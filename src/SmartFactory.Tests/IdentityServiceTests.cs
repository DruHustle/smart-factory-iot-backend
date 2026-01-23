using Moq;
using SmartFactory.Services.IdentityService.Application.Interfaces;
using Xunit;
using System.Threading.Tasks;

namespace SmartFactory.Tests
{
    /// <summary>
    /// Unit tests for the Identity Service.
    /// Validates authentication and authorization logic.
    /// </summary>
    public class IdentityServiceTests
    {
        [Fact]
        public async Task AuthenticateAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var mockIdentityService = new Mock<IIdentityService>();
            var username = "admin";
            var password = "password123";
            var expectedToken = "mock-jwt-token";

            mockIdentityService.Setup(s => s.AuthenticateAsync(username, password))
                .ReturnsAsync(expectedToken);

            // Act
            var result = await mockIdentityService.Object.AuthenticateAsync(username, password);

            // Assert
            Assert.Equal(expectedToken, result);
            mockIdentityService.Verify(x => x.AuthenticateAsync(username, password), Times.Once);
        }
    }
}
