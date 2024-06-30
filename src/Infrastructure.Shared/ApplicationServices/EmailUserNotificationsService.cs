using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a <see cref="IUserNotificationsService" /> that delivers notifications via asynchronous email delivery
///     using
///     <see cref="IEmailSchedulingService" />
/// </summary>
public class EmailUserNotificationsService : IUserNotificationsService
{
    private const string ProductNameSettingName = "ApplicationServices:EmailNotifications:SenderProductName";
    private const string SenderDisplayNameSettingName = "ApplicationServices:EmailNotifications:SenderDisplayName";
    private const string SenderEmailAddressSettingName = "ApplicationServices:EmailNotifications:SenderEmailAddress";
    private readonly IEmailSchedulingService _emailSchedulingService;
    private readonly IHostSettings _hostSettings;
    private readonly string _productName;
    private readonly string _senderEmailAddress;
    private readonly string _senderName;
    private readonly IWebsiteUiService _websiteUiService;

    public EmailUserNotificationsService(IConfigurationSettings settings, IHostSettings hostSettings,
        IWebsiteUiService websiteUiService, IEmailSchedulingService emailSchedulingService)
    {
        _hostSettings = hostSettings;
        _websiteUiService = websiteUiService;
        _emailSchedulingService = emailSchedulingService;
        _productName = settings.Platform.GetString(ProductNameSettingName, nameof(EmailUserNotificationsService));
        _senderEmailAddress =
            settings.Platform.GetString(SenderEmailAddressSettingName, nameof(EmailUserNotificationsService));
        _senderName = settings.Platform.GetString(SenderDisplayNameSettingName, nameof(EmailUserNotificationsService));
    }

    public async Task<Result<Error>> NotifyGuestInvitationToPlatformAsync(ICallerContext caller, string token,
        string inviteeEmailAddress,
        string inviteeName, string inviterName, CancellationToken cancellationToken)
    {
        var webSiteUrl = _hostSettings.GetWebsiteHostBaseUrl();
        var webSiteRoute = _websiteUiService.CreateRegistrationPageUrl(token);
        var link = webSiteUrl.WithoutTrailingSlash() + webSiteRoute;
        var htmlBody =
            $"""
             <p>Hello,</p>
             <p>You have been invited by {inviterName} to {_productName}.</p>
             <p>Please click this link to <a href="{link}">sign up</a></p>
             <p>This is an automated email from the support team at {_productName}</p>
             """;

        return await _emailSchedulingService.ScheduleHtmlEmail(caller, new HtmlEmail
        {
            Subject = $"Welcome to {_productName}",
            Body = htmlBody,
            FromEmailAddress = _senderEmailAddress,
            FromDisplayName = _senderName,
            ToEmailAddress = inviteeEmailAddress,
            ToDisplayName = inviteeName
        }, cancellationToken);
    }

    public async Task<Result<Error>> NotifyPasswordRegistrationConfirmationAsync(ICallerContext caller,
        string emailAddress, string name, string token,
        CancellationToken cancellationToken)
    {
        var webSiteUrl = _hostSettings.GetWebsiteHostBaseUrl();
        var webSiteRoute = _websiteUiService.ConstructPasswordRegistrationConfirmationPageUrl(token);
        var link = webSiteUrl.WithoutTrailingSlash() + webSiteRoute;
        var htmlBody =
            $"""
             <p>Hello {name},</p>
             <p>Thank you for signing up at {_productName}.</p>
             <p>Please click this link to <a href="{link}">confirm your email address</a></p>
             <p>This is an automated email from the support team at {_productName}</p>
             """;

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

    public async Task<Result<Error>> NotifyPasswordRegistrationRepeatCourtesyAsync(ICallerContext caller, string userId,
        string emailAddress, string name, string? timezone, string? countryCode, CancellationToken cancellationToken)
    {
        var htmlBody =
            $"""
             <p>Hello {name},</p>
             <p>We have received a request to register a person using your email address at our web site {_productName}.</p>
             <p>Of course, your email address ('{emailAddress}') has already been registered at our site.</p>
             <p>If you are already aware of this activity, then there is nothing more to do.</p>
             <p>It is possible that some unknown party is trying to find out if your email address is already registered on this site, by trying to re-register it.</p>
             <p>We have blocked this attempt from succeeding, and no new account has been created. Your account is still safe.</p>
             <p>We just thought you would like to know, that this is going on. There is nothing more you need to do.</p>
             <p>This is an automated email from the support team at {_productName}</p>
             """;

        return await _emailSchedulingService.ScheduleHtmlEmail(caller, new HtmlEmail
        {
            Subject = $"{_productName} Account Registration Attempt",
            Body = htmlBody,
            FromEmailAddress = _senderEmailAddress,
            FromDisplayName = _senderName,
            ToEmailAddress = emailAddress,
            ToDisplayName = name
        }, cancellationToken);
    }

    public async Task<Result<Error>> NotifyPasswordResetInitiatedAsync(ICallerContext caller, string name,
        string emailAddress, string token,
        CancellationToken cancellationToken)
    {
        var webSiteUrl = _hostSettings.GetWebsiteHostBaseUrl();
        var webSiteRoute = _websiteUiService.ConstructPasswordResetConfirmationPageUrl(token);
        var link = webSiteUrl.WithoutTrailingSlash() + webSiteRoute;
        var htmlBody =
            $$"""
              <p>Hello {{name}},</p>
              <p>We have received a request to reset your password at {{_productName}}.</p>
              <p>If you did not make this request, please contact the {{_productName}} support team immediately.</p>
              <p></p>
              <p>If you expected this email, please click this link to <a href="{{link}}">reset your password</a></p>
              <p>This is an automated email from the support team at {{_productName}}</p>
              """;

        return await _emailSchedulingService.ScheduleHtmlEmail(caller, new HtmlEmail
        {
            Subject = $"Reset your {_productName} password",
            Body = htmlBody,
            FromEmailAddress = _senderEmailAddress,
            FromDisplayName = _senderName,
            ToEmailAddress = emailAddress,
            ToDisplayName = name
        }, cancellationToken);
    }

    public async Task<Result<Error>> NotifyPasswordResetUnknownUserCourtesyAsync(ICallerContext caller,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        var htmlBody =
            $"""
             <p>Hello,</p>
             <p>We have received a very suspicious request to reset your password at our web site {_productName}.</p>
             <p>You have no registered account at the web site of {_productName}, so you are safe.</p>
             <p>It is possible that some suspicious party is trying to access your account through our web site, but it does not exist.</p>
             <p></p>
             <p>There is nothing more for you to do.</p>
             <p>We have blocked this attempt from succeeding.</p>
             <p>We just thought you would like to know, that this is going on.</p>
             <p>This is an automated email from the support team at {_productName}</p>
             """;

        return await _emailSchedulingService.ScheduleHtmlEmail(caller, new HtmlEmail
        {
            Subject = $"{_productName} Account Registration Attempt",
            Body = htmlBody,
            FromEmailAddress = _senderEmailAddress,
            FromDisplayName = _senderName,
            ToEmailAddress = emailAddress,
            ToDisplayName = emailAddress
        }, cancellationToken);
    }
}