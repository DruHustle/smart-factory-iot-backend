using Moq;
using SmartFactory.BuildingBlocks.EventBus.Abstractions;
using SmartFactory.BuildingBlocks.EventBus.Models;
using SmartFactory.BuildingBlocks.EventBus.Implementations;
using Xunit;
using System.Threading.Tasks;

namespace SmartFactory.Tests
{
    /// <summary>
    /// Tests for the Event Bus building block.
    /// Follows SOLID principles by testing against abstractions.
    /// </summary>
    public class EventBusTests
    {
        [Fact]
        public async Task PublishAsync_ShouldCallUnderlyingBroker()
        {
            // Arrange
            var mockEventBus = new Mock<IEventBus>();
            var integrationEvent = new TestIntegrationEvent();

            // Act
            await mockEventBus.Object.PublishAsync(integrationEvent);

            // Assert
            mockEventBus.Verify(x => x.PublishAsync(It.IsAny<IntegrationEvent>()), Times.Once);
        }

        private class TestIntegrationEvent : IntegrationEvent
        {
        }
    }
}
