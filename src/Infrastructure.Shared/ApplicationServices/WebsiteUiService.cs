using Application.Services.Shared;
using Common.Extensions;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a service for constructing resources based on a known Website UI Application
/// </summary>
public sealed class WebsiteUiService : IWebsiteUiService
{
    //EXTEND: these URLs must reflect those used by the website that handles UI 
    public const string LoginPageRoute = "/login";
    public const string OAuth2ConsentPageRoute = "/oauth2/authorize/consent";
    public const string PasswordMfaOobConfirmationPageRoute = "/confirm-password-mfaoob";
    public const string PasswordRegistrationConfirmationPageRoute = "/confirm-password-registration";
    public const string PasswordResetConfirmationPageRoute = "/confirm-password-reset";
    public const string RegistrationPageRoute = "/register";

    public string ConstructLoginPageUrl()
    {
        return LoginPageRoute;
    }

    public string ConstructOAuth2ConsentPageUrl(string clientId, string scope)
    {
        return $"{OAuth2ConsentPageRoute.WithoutTrailingSlash()}?client_id={clientId}&scope={scope}";
    }

    public string ConstructPasswordMfaOobConfirmationPageUrl(string code)
    {
        var escapedCode = Uri.EscapeDataString(code);
        return $"{PasswordMfaOobConfirmationPageRoute}?code={escapedCode}";
    }

    public string ConstructPasswordRegistrationConfirmationPageUrl(string token)
    {
        var escapedToken = Uri.EscapeDataString(token);
        return $"{PasswordRegistrationConfirmationPageRoute}?token={escapedToken}";
    }

    public string ConstructPasswordResetConfirmationPageUrl(string token)
    {
        var escapedToken = Uri.EscapeDataString(token);
        return $"{PasswordResetConfirmationPageRoute}?token={escapedToken}";
    }

    public string CreateRegistrationPageUrl(string token)
    {
        var escapedToken = Uri.EscapeDataString(token);
        return $"{RegistrationPageRoute}?token={escapedToken}";
    }
}