using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace SmartFactory.Services.NotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly GraphServiceClient _graphServiceClient;

        public NotificationsController(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
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
            return Ok(new { Status = "Email sent successfully" });
        }

        [HttpPost("trigger-logic-app")]
        public async Task<IActionResult> TriggerLogicApp([FromBody] AlertPayload payload)
        {
            // Logic to trigger an Azure Logic App via HTTP POST
            // This would typically use an HttpClient to call the Logic App's trigger URL
            return Ok(new { Status = "Logic App triggered", Alert = payload.AlertName });
        }
    }

    public class EmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public class AlertPayload
    {
        public string AlertName { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
