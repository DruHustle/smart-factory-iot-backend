using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartFactory.Services.DeviceService.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartFactory.Services.DeviceService.Infrastructure.BackgroundServices
{
    public class UpdateManagerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UpdateManagerService> _logger;

        public UpdateManagerService(IServiceProvider serviceProvider, ILogger<UpdateManagerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Update Manager Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                        var devices = await repository.GetAllAsync();
                        
                        var pendingDevices = devices.Where(d => d.UpdateStatus == "Pending").ToList();

                        foreach (var device in pendingDevices)
                        {
                            _logger.LogInformation($"Processing update for device {device.DeviceId} to version {device.PendingUpdateVersion}");
                            
                            // Simulate update process
                            device.UpdateStatus = "Downloading";
                            await repository.UpdateAsync(device);
                            await Task.Delay(2000, stoppingToken);

                            device.UpdateStatus = "Installing";
                            await repository.UpdateAsync(device);
                            await Task.Delay(2000, stoppingToken);

                            device.UpdateStatus = "Completed";
                            device.FirmwareVersion = device.PendingUpdateVersion ?? device.FirmwareVersion;
                            device.PendingUpdateVersion = null;
                            device.LastUpdateDate = DateTime.UtcNow;
                            await repository.UpdateAsync(device);

                            _logger.LogInformation($"Update completed for device {device.DeviceId}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing updates.");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            _logger.LogInformation("Update Manager Service is stopping.");
        }
    }
}
