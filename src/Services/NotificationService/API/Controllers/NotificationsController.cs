using Microsoft.AspNetCore.Mvc;
using SmartFactory.Services.NotificationService.Application.DTOs;
using SmartFactory.Services.NotificationService.Application.Interfaces;

namespace SmartFactory.Services.NotificationService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            await _notificationService.SendEmailAsync(request);
            return Ok(new { Status = "Email sent successfully" });
        }

        [HttpPost("trigger-logic-app")]
        public async Task<IActionResult> TriggerLogicApp([FromBody] AlertPayload payload)
        {
            await _notificationService.TriggerLogicAppAsync(payload);
            return Ok(new { Status = "Logic App triggered", Alert = payload.AlertName });
        }
    }
}
