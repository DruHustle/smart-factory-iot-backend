namespace SmartFactory.Services.IdentityService.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for identity-related operations, following the Interface Segregation Principle.
    /// </summary>
    public interface IIdentityService
    {
        /// <summary>
        /// Acquires an access token for the current authenticated user for specific scopes.
        /// </summary>
        Task<string> GetTokenForUserAsync(string[] scopes);

        /// <summary>
        /// Extracts and returns the user profile information from the claims principal.
        /// </summary>
        object GetUserProfile(System.Security.Claims.ClaimsPrincipal user);

        /// <summary>
        /// Validates if the user has a specific role.
        /// </summary>
        bool IsInRole(System.Security.Claims.ClaimsPrincipal user, string role);

        /// <summary>
        /// Gets the unique identifier of the current user.
        /// </summary>
        string? GetUserId(System.Security.Claims.ClaimsPrincipal user);
    }
}
