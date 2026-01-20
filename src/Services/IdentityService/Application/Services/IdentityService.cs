using Microsoft.Identity.Web;
using SmartFactory.Services.IdentityService.Application.Interfaces;
using System.Security.Claims;

namespace SmartFactory.Services.IdentityService.Application.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        public IdentityService(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        public async Task<string> GetTokenForUserAsync(string[] scopes)
        {
            return await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
        }

        public object GetUserProfile(ClaimsPrincipal user)
        {
            return new
            {
                Name = user.Identity?.Name,
                IsAuthenticated = user.Identity?.IsAuthenticated,
                Claims = user.Claims.Select(c => new { c.Type, c.Value })
            };
        }
    }
}
