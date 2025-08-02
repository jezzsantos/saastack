namespace Application.Services.Shared;

/// <summary>
///     Defines a service for constructing resources based on a known Website UI Application
/// </summary>
public interface IWebsiteUiService
{
    string ConstructPasswordMfaOobConfirmationPageUrl(string code);

    string ConstructPasswordRegistrationConfirmationPageUrl(string token);

    string ConstructPasswordResetConfirmationPageUrl(string token);

    string CreateRegistrationPageUrl(string token);

    string ConstructOAuth2ConsentPageUrl(string clientId, string scope);

    string ConstructLoginPageUrl();
}