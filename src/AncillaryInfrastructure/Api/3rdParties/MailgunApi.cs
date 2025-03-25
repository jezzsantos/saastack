using AncillaryApplication;
using Application.Common;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.External.ApplicationServices;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Mailgun;
using Microsoft.AspNetCore.Http;

namespace AncillaryInfrastructure.Api._3rdParties;

public class MailgunApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMailgunApplication _mailgunApplication;
    private readonly IRecorder _recorder;
    private readonly string _webhookSigningKey;

    public MailgunApi(IRecorder recorder, IHttpContextAccessor httpContextAccessor, ICallerContextFactory callerFactory,
        IConfigurationSettings settings, IMailgunApplication mailgunApplication)
    {
        _recorder = recorder;
        _httpContextAccessor = httpContextAccessor;
        _callerFactory = callerFactory;
        _mailgunApplication = mailgunApplication;
        _webhookSigningKey = settings.Platform.GetString(MailgunClient.Constants.WebhookSigningKeySettingName);
    }

    /// <summary>
    ///     Authenticates the request with Mailgun HMAC auth
    ///     See <see href="https://documentation.mailgun.com/docs/mailgun/user-manual/tracking-messages/#securing-webhooks" />
    /// </summary>
    internal static Result<Error> AuthenticateRequest(IRecorder recorder, ICallerContext caller,
        IHttpContextAccessor httpContextAccessor, MailgunSignature? parameters, string webhookSigningKey)
    {
        var httpContext = httpContextAccessor.HttpContext!;
        if (!httpContext.Request.IsHttps)
        {
            recorder.TraceWarning(caller.ToCall(), "MailgunApi authentication is not secured with HTTPS");
            return Error.NotAuthenticated();
        }

        if (webhookSigningKey.HasNoValue())
        {
            recorder.TraceWarning(caller.ToCall(), "MailgunApi authentication is misconfigured");
            return Error.NotAuthenticated();
        }

        if (parameters.NotExists()
            || parameters.Signature.HasNoValue())
        {
            recorder.Audit(caller.ToCall(), Application.Interfaces.Audits.MailgunApi_WebhookAuthentication_Failed,
                "Mailgun webhook failed authentication");
            return Error.NotAuthenticated();
        }

        var signature = $"sha256={parameters.Signature}";
        var text = $"{parameters.Timestamp}{parameters.Token}";

        var signer = new HMACSigner(text, webhookSigningKey);
        var verifier = new HMACVerifier(signer);
        var verified = verifier.Verify(signature);
        if (!verified)
        {
            recorder.Audit(caller.ToCall(), Application.Interfaces.Audits.MailgunApi_WebhookAuthentication_Failed,
                "Mailgun webhook failed authentication");
            return Error.NotAuthenticated();
        }

        return Result.Ok;
    }

    public async Task<ApiEmptyResult> NotifyWebhookEvent(MailgunNotifyWebhookEventRequest request,
        CancellationToken cancellationToken)
    {
        var caller = _callerFactory.Create();
        var authenticated =
            AuthenticateRequest(_recorder, caller, _httpContextAccessor, request.Signature, _webhookSigningKey);
        if (authenticated.IsFailure)
        {
            return () => authenticated.Error;
        }

        if (request.EventData.NotExists()
            || request.EventData.Event.HasNoValue())
        {
            return () => new EmptyResponse();
        }

        var maintenance = Caller.CreateAsMaintenance(caller.CallId);
        var notified =
            await _mailgunApplication.NotifyWebhookEvent(maintenance, request.EventData, cancellationToken);

        return () => notified.Match(() => new EmptyResponse(),
            error => new Result<EmptyResponse, Error>(error));
    }
}