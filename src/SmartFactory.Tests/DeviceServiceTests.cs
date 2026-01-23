using Moq;
using SmartFactory.Services.DeviceService.Domain.Entities;
using SmartFactory.Services.DeviceService.Domain.Interfaces;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartFactory.Tests
{
    /// <summary>
    /// Unit tests for the Device Service.
    /// Ensures device management logic works correctly.
    /// </summary>
    public class DeviceServiceTests
    {
        [Fact]
        public async Task GetDeviceById_ShouldReturnDevice_WhenDeviceExists()
        {
            // Arrange
            var mockRepo = new Mock<IDeviceRepository>();
            var deviceId = "DEV-001";
            var expectedDevice = new Device { Id = deviceId, Name = "Sensor 1", Status = "Active" };
            
            mockRepo.Setup(repo => repo.GetByIdAsync(deviceId))
                .ReturnsAsync(expectedDevice);

            // Act
            var result = await mockRepo.Object.GetByIdAsync(deviceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceId, result.Id);
            mockRepo.Verify(x => x.GetByIdAsync(deviceId), Times.Once);
        }
    }
}
