using AncillaryApplication;
using Common;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Shared.ApplicationServices.External;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Mailgun;

namespace AncillaryInfrastructure.Api._3rdParties;

public class MailgunApi : IWebApiService
{
    private readonly IAncillaryApplication _ancillaryApplication;
    private readonly ICallerContextFactory _callerFactory;
    private readonly string _webhookSigningKey;

    public MailgunApi(ICallerContextFactory callerFactory, IAncillaryApplication ancillaryApplication,
        IConfigurationSettings settings)
    {
        _callerFactory = callerFactory;
        _ancillaryApplication = ancillaryApplication;
        _webhookSigningKey = settings.Platform.GetString(MailgunConstants.WebhookSigningKeySettingName);
    }

    public async Task<ApiEmptyResult> NotifyEmailDeliveryReceipt(MailgunNotifyWebhookEventRequest request,
        CancellationToken cancellationToken)
    {
        var authenticated = AuthenticateRequest(request.Signature, _webhookSigningKey);
        if (authenticated.IsFailure)
        {
            return () => authenticated.Error;
        }

        if (!request.EventData.Exists())
        {
            return () => new EmptyResponse();
        }

        if (request.EventData.Event == MailgunConstants.Events.Delivered)
        {
            var deliveredAt = request.EventData.Timestamp.FromUnixTimestamp();
            var receiptId = request.EventData.Message?.Headers?.MessageId;
            if (receiptId.HasNoValue())
            {
                return () => new EmptyResponse();
            }

            var delivered = await _ancillaryApplication.ConfirmEmailDeliveredAsync(_callerFactory.Create(),
                receiptId, deliveredAt, cancellationToken);

            return () => delivered.Match(() => new EmptyResponse(),
                error => new Result<EmptyResponse, Error>(error));
        }

        if (request.EventData.Event == MailgunConstants.Events.Failed)
        {
            var severity = request.EventData.Severity ?? MailgunConstants.Values.PermanentSeverity;
            if (severity.NotEqualsIgnoreCase(MailgunConstants.Values.PermanentSeverity))
            {
                return () => new EmptyResponse();
            }

            var failedAt = request.EventData.Timestamp.FromUnixTimestamp();
            var reason = request.EventData.DeliveryStatus?.Description ?? request.EventData.Reason ?? "none";
            var receiptId = request.EventData.Message?.Headers?.MessageId;
            if (receiptId.HasNoValue())
            {
                return () => new EmptyResponse();
            }

            var delivered = await _ancillaryApplication.ConfirmEmailDeliveryFailedAsync(_callerFactory.Create(),
                receiptId, failedAt, reason, cancellationToken);

            return () => delivered.Match(() => new EmptyResponse(),
                error => new Result<EmptyResponse, Error>(error));
        }

        return () => new EmptyResponse();
    }

    /// <summary>
    ///     Authenticates the request with Mailgun HMAC auth
    ///     See <see href="https://documentation.mailgun.com/docs/mailgun/user-manual/tracking-messages/#securing-webhooks" />
    /// </summary>
    private static Result<Error> AuthenticateRequest(MailgunSignature? parameters, string webhookSigningKey)
    {
        if (parameters.NotExists()
            || parameters.Signature.HasNoValue())
        {
            return Error.NotAuthenticated();
        }

        var signature = $"sha1={parameters.Signature}";
        var text = $"{parameters.Timestamp}{parameters.Token}";

        var signer = new HMACSigner(text, webhookSigningKey);
        var verifier = new HMACVerifier(signer);
        var verified = verifier.Verify(signature);
        if (!verified)
        {
            return Error.NotAuthenticated();
        }

        return Result.Ok;
    }
}