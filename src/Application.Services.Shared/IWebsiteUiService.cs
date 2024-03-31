namespace Application.Services.Shared;

/// <summary>
///     Defines a service for constructing resources based on a known Website UI Application
/// </summary>
public interface IWebsiteUiService
{
    string ConstructPasswordRegistrationConfirmationPageUrl(string token);

    string CreateRegistrationPageUrl(string token);
}