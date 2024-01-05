using Application.Services.Shared;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a service for constructing resources based on a known Website UI Application
/// </summary>
public sealed class WebsiteUiService : IWebsiteUiService
{
    private const string RegistrationConfirmationPageRoute = "/confirm-registeration";

    public string ConstructPasswordRegistrationConfirmationPageUrl(string token)
    {
        var escapedToken = Uri.EscapeDataString(token);
        return $"{RegistrationConfirmationPageRoute}?token={escapedToken}";
    }
}