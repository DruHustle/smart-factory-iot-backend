using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authorization;

namespace SmartFactory.Services.IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        public AuthController(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        [HttpGet("token")]
        [Authorize]
        public async Task<IActionResult> GetToken()
        {
            // This endpoint demonstrates how to acquire a token for downstream APIs (e.g., Graph API)
            // using the on-behalf-of flow with Microsoft Entra ID.
            string[] scopes = new[] { "User.Read" };
            string accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
            return Ok(new { AccessToken = accessToken });
        }

        [HttpGet("profile")]
        [Authorize]
        public IActionResult GetProfile()
        {
            var user = User.Identity;
            return Ok(new { Name = user?.Name, IsAuthenticated = user?.IsAuthenticated });
        }
    }
}
