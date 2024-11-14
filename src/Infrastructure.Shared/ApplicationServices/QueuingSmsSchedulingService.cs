using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Services.Shared;
using Common;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a queue scheduling service, that will schedule SMS messages for asynchronous and deferred delivery
/// </summary>
public class QueuingSmsSchedulingService : ISmsSchedulingService
{
    private readonly ISmsMessageQueue _queue;
    private readonly IRecorder _recorder;

    public QueuingSmsSchedulingService(IRecorder recorder, ISmsMessageQueue queue)
    {
        _recorder = recorder;
        _queue = queue;
    }

    public async Task<Result<Error>> ScheduleSms(ICallerContext caller, SmsText smsText,
        CancellationToken cancellationToken)
    {
        var queued = await _queue.PushAsync(caller.ToCall(), new SmsMessage
        {
            Message = new QueuedSmsMessage
            {
                Body = smsText.Body,
                ToPhoneNumber = smsText.To,
                Tags = smsText.Tags
            }
        }, cancellationToken);
        if (queued.IsFailure)
        {
            return queued.Error;
        }

        var message = queued.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Pended SMS message {Id} for {To} with tags {Tags}", message.MessageId!,
            smsText.To, smsText.Tags ?? new List<string> { "(none)" });

        return Result.Ok;
    }
}