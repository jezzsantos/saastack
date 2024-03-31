using Application.Services.Shared;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a service for constructing resources based on a known Website UI Application
/// </summary>
public sealed class WebsiteUiService : IWebsiteUiService
{
    //EXTEND: these URLs must reflect those used by the website that handles UI 
    private const string PasswordRegistrationConfirmationPageRoute = "/confirm-password-registration";
    private const string RegistrationPageRoute = "/register";

    public string ConstructPasswordRegistrationConfirmationPageUrl(string token)
    {
        var escapedToken = Uri.EscapeDataString(token);
        return $"{PasswordRegistrationConfirmationPageRoute}?token={escapedToken}";
    }

    public string CreateRegistrationPageUrl(string token)
    {
        var escapedToken = Uri.EscapeDataString(token);
        return $"{RegistrationPageRoute}?token={escapedToken}";
    }
}