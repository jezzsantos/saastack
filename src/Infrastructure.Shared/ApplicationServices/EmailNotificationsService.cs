using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a <see cref="INotificationsService" /> that delivers notifications via asynchronous email delivery using
///     <see cref="IEmailSchedulingService" />
/// </summary>
public class EmailNotificationsService : INotificationsService
{
    public const string ProductNameSettingName = "ApplicationServices:Notifications:SenderProductName";
    public const string SenderDisplayNameSettingName = "ApplicationServices:Notifications:SenderDisplayName";
    public const string SenderEmailAddressSettingName = "ApplicationServices:Notifications:SenderEmailAddress";
    private readonly IEmailSchedulingService _emailSchedulingService;
    private readonly IHostSettings _hostSettings;
    private readonly string _productName;
    private readonly string _senderEmailAddress;
    private readonly string _senderName;
    private readonly IWebsiteUiService _websiteUiService;

    public EmailNotificationsService(IConfigurationSettings settings, IHostSettings hostSettings,
        IWebsiteUiService websiteUiService, IEmailSchedulingService emailSchedulingService)
    {
        _hostSettings = hostSettings;
        _websiteUiService = websiteUiService;
        _emailSchedulingService = emailSchedulingService;
        _productName = settings.Platform.GetString(ProductNameSettingName, nameof(EmailNotificationsService));
        _senderEmailAddress =
            settings.Platform.GetString(SenderEmailAddressSettingName, nameof(EmailNotificationsService));
        _senderName = settings.Platform.GetString(SenderDisplayNameSettingName, nameof(EmailNotificationsService));
    }

    public async Task<Result<Error>> NotifyPasswordRegistrationConfirmationAsync(ICallerContext caller,
        string emailAddress, string name, string token,
        CancellationToken cancellationToken)
    {
        var webSiteUrl = _hostSettings.GetWebsiteHostBaseUrl();
        var webSiteRoute = _websiteUiService.ConstructPasswordRegistrationConfirmationPageUrl(token);
        var link = webSiteUrl.WithoutTrailingSlash() + webSiteRoute;
        var htmlBody =
            $"<p>Hello {name},</p>" +
            $"<p>Thank you for signing up at {_productName}.</p>" +
            $"<p>Please click this link to <a href=\"{link}\">confirm your email address</a></p>" +
            $"<p>This is an automated email from the support team at {_productName}</p>";

        return await _emailSchedulingService.ScheduleHtmlEmail(caller, new HtmlEmail
        {
            Subject = $"Welcome to {_productName}",
            Body = htmlBody,
            FromEmailAddress = _senderEmailAddress,
            FromDisplayName = _senderName,
            ToEmailAddress = emailAddress,
            ToDisplayName = name
        }, cancellationToken);
    }
}