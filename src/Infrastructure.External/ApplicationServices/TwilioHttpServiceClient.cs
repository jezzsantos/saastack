using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Common;
using Common.Configuration;

namespace Infrastructure.External.ApplicationServices;

/// <summary>
///     Provides an adapter to the Programmable Messaging services of Twilio.com
///     <see href="https://www.twilio.com/docs/messaging/api/message-resource#create-a-message-resource" />
/// </summary>
public class TwilioHttpServiceClient : ISmsDeliveryService
{
    private readonly IRecorder _recorder;
    private readonly ITwilioClient _serviceClient;

    public TwilioHttpServiceClient(IRecorder recorder, IConfigurationSettings settings,
        IHttpClientFactory httpClientFactory) : this(recorder,
        new TwilioClient(recorder, settings, httpClientFactory))
    {
    }

    public TwilioHttpServiceClient(IRecorder recorder, ITwilioClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
    }

    public async Task<Result<SmsDeliveryReceipt, Error>> SendAsync(ICallerContext caller, string body,
        string toPhoneNumber, IReadOnlyList<string>? tags, CancellationToken cancellationToken)
    {
        _recorder.TraceInformation(caller.ToCall(), "Sending SMS to {To} in Twilio", toPhoneNumber);

        var sent = await _serviceClient.SendAsync(caller.ToCall(), toPhoneNumber, body, tags, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Sent SMS to {To} in Twilio successfully",
            toPhoneNumber);

        return sent.Value;
    }
}