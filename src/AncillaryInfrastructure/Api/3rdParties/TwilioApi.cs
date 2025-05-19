using AncillaryApplication;
using Application.Common;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Twilio;

namespace AncillaryInfrastructure.Api._3rdParties;

public class TwilioApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly ITwilioApplication _twilioApplication;

    public TwilioApi(ICallerContextFactory callerFactory, ITwilioApplication twilioApplication)
    {
        _callerFactory = callerFactory;
        _twilioApplication = twilioApplication;
    }

    public async Task<ApiEmptyResult> NotifyWebhookEvent(TwilioNotifyWebhookEventRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MessageSid.HasNoValue())
        {
            return () => new EmptyResponse();
        }

        var caller = _callerFactory.Create();
        var eventData = request.Convert<TwilioNotifyWebhookEventRequest, TwilioEventData>();
        var maintenance = Caller.CreateAsMaintenance(caller);
        var notified =
            await _twilioApplication.NotifyWebhookEvent(maintenance, eventData, cancellationToken);

        return () => notified.Match(() => new EmptyResponse(),
            error => new Result<EmptyResponse, Error>(error));
    }
}