using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Services.Shared;
using Common;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides an queue scheduling service, that will schedule messages for asynchronous and deferred delivery
/// </summary>
public class QueuingEmailSchedulingService : IEmailSchedulingService
{
    private readonly IEmailMessageQueue _queue;
    private readonly IRecorder _recorder;

    public QueuingEmailSchedulingService(IRecorder recorder, IEmailMessageQueue queue)
    {
        _recorder = recorder;
        _queue = queue;
    }

    public async Task<Result<Error>> ScheduleHtmlEmail(ICallerContext caller, HtmlEmail htmlEmail,
        CancellationToken cancellationToken)
    {
        var queued = await _queue.PushAsync(caller.ToCall(), new EmailMessage
        {
            Html = new QueuedEmailHtmlMessage
            {
                Subject = htmlEmail.Subject,
                FromEmailAddress = htmlEmail.FromEmailAddress,
                FromDisplayName = htmlEmail.FromDisplayName,
                HtmlBody = htmlEmail.Body,
                ToEmailAddress = htmlEmail.ToEmailAddress,
                ToDisplayName = htmlEmail.ToDisplayName
            }
        }, cancellationToken);
        if (!queued.IsSuccessful)
        {
            return queued.Error;
        }

        var message = queued.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Pended email message {Id} for {To} with subject {Subject}", message.MessageId!,
            htmlEmail.ToEmailAddress, htmlEmail.Subject);

        return Result.Ok;
    }
}