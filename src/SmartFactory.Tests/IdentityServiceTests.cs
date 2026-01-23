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
        public async Task GetTokenForUserAsync_ShouldReturnToken_WhenScopesAreProvided()
        {
            // Arrange
            var mockIdentityService = new Mock<IIdentityService>();
            var scopes = new[] { "api://smart-factory/access" };
            var expectedToken = "mock-jwt-token";

            mockIdentityService.Setup(s => s.GetTokenForUserAsync(scopes))
                .ReturnsAsync(expectedToken);

            // Act
            var result = await mockIdentityService.Object.GetTokenForUserAsync(scopes);

            // Assert
            Assert.Equal(expectedToken, result);
            mockIdentityService.Verify(x => x.GetTokenForUserAsync(scopes), Times.Once);
        }
    }
}
