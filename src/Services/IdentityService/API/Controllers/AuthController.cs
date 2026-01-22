using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFactory.Services.IdentityService.Application.Interfaces;

namespace SmartFactory.Services.IdentityService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityService _identityService;

        public AuthController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        [HttpGet("token")]
        [Authorize]
        public async Task<IActionResult> GetToken()
        {
            string[] scopes = new[] { "User.Read" };
            var accessToken = await _identityService.GetTokenForUserAsync(scopes);
            return Ok(new { AccessToken = accessToken });
        }

        [HttpGet("profile")]
        [Authorize]
        public IActionResult GetProfile()
        {
            var profile = _identityService.GetUserProfile(User);
            return Ok(profile);
        }

        [HttpGet("check-role/{role}")]
        [Authorize]
        public IActionResult CheckRole(string role)
        {
            var hasRole = _identityService.IsInRole(User, role);
            return Ok(new { Role = role, HasRole = hasRole });
        }
    }
}
