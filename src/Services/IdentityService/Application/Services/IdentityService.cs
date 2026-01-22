using Microsoft.Identity.Web;
using SmartFactory.Services.IdentityService.Application.Interfaces;
using System.Security.Claims;

namespace SmartFactory.Services.IdentityService.Application.Services
{
    /// <summary>
    /// Implementation of the IIdentityService using Microsoft Identity Web.
    /// Follows the Single Responsibility Principle by focusing on identity and token management.
    /// </summary>
    public class IdentityService : IIdentityService
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        public IdentityService(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        /// <inheritdoc/>
        public async Task<string> GetTokenForUserAsync(string[] scopes)
        {
            try
            {
                return await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
            }
            catch (Exception ex)
            {
                // In a real application, we would log this error
                throw new InvalidOperationException("Failed to acquire token for user.", ex);
            }
        }

        /// <inheritdoc/>
        public object GetUserProfile(ClaimsPrincipal user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            return new
            {
                UserId = GetUserId(user),
                Name = user.Identity?.Name,
                Email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("preferred_username")?.Value,
                IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
                Roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList(),
                Claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList()
            };
        }

        /// <inheritdoc/>
        public bool IsInRole(ClaimsPrincipal user, string role)
        {
            return user?.IsInRole(role) ?? false;
        }

        /// <inheritdoc/>
        public string? GetUserId(ClaimsPrincipal user)
        {
            // Try to find the standard NameIdentifier claim or the Azure AD 'oid' (Object ID) claim
            return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? user?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        }
    }
}
