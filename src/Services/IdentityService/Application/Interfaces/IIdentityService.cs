namespace SmartFactory.Services.IdentityService.Application.Interfaces
{
    public interface IIdentityService
    {
        Task<string> GetTokenForUserAsync(string[] scopes);
        object GetUserProfile(System.Security.Claims.ClaimsPrincipal user);
    }
}
