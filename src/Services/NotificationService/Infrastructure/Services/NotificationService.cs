using Microsoft.Graph;
using SmartFactory.Services.NotificationService.Application.DTOs;
using SmartFactory.Services.NotificationService.Application.Interfaces;

namespace SmartFactory.Services.NotificationService.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly GraphServiceClient _graphServiceClient;

        public NotificationService(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        public async Task SendEmailAsync(EmailRequest request)
        {
            var message = new Message
            {
                Subject = request.Subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = request.Body
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = request.To
                        }
                    }
                }
            };

            await _graphServiceClient.Me.SendMail(message, null).Request().PostAsync();
        }

        public async Task TriggerLogicAppAsync(AlertPayload payload)
        {
            // Logic to trigger an Azure Logic App via HTTP POST
            await Task.CompletedTask;
        }
    }
}
