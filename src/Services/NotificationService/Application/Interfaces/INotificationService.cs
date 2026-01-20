using SmartFactory.Services.NotificationService.Application.DTOs;

namespace SmartFactory.Services.NotificationService.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendEmailAsync(EmailRequest request);
        Task TriggerLogicAppAsync(AlertPayload payload);
    }
}
