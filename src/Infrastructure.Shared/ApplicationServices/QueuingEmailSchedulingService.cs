using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Services.Shared;
using Common;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a queue scheduling service, that will schedule email messages for asynchronous and deferred delivery
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

    public async Task<Result<Error>> ScheduleHtmlEmail(ICallerContext caller, HtmlEmail email,
        CancellationToken cancellationToken)
    {
        var queued = await _queue.PushAsync(caller.ToCall(), new EmailMessage
        {
            Html = new QueuedEmailHtmlMessage
            {
                Subject = email.Subject,
                Body = email.Body,
                FromEmailAddress = email.FromEmailAddress,
                FromDisplayName = email.FromDisplayName,
                ToEmailAddress = email.ToEmailAddress,
                ToDisplayName = email.ToDisplayName,
                Tags = email.Tags
            }
        }, cancellationToken);
        if (queued.IsFailure)
        {
            return queued.Error;
        }

        var message = queued.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Pended HTML email message {Id} for {To} with subject {Subject}, and tags {Tags}", message.MessageId!,
            email.ToEmailAddress, email.Subject, email.Tags ?? ["(none)"]);

        return Result.Ok;
    }

    public async Task<Result<Error>> ScheduleTemplatedEmail(ICallerContext caller, TemplatedEmail email,
        CancellationToken cancellationToken)
    {
        var queued = await _queue.PushAsync(caller.ToCall(), new EmailMessage
        {
            Template = new QueuedEmailTemplatedMessage
            {
                TemplateId = email.TemplateId,
                Subject = email.Subject,
                Substitutions = email.Substitutions,
                FromEmailAddress = email.FromEmailAddress,
                FromDisplayName = email.FromDisplayName,
                ToEmailAddress = email.ToEmailAddress,
                ToDisplayName = email.ToDisplayName,
                Tags = email.Tags
            }
        }, cancellationToken);
        if (queued.IsFailure)
        {
            return queued.Error;
        }

        var message = queued.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Pended templated email message {Id} for {To} with template {Template}, and tags {Tags}",
            message.MessageId!,
            email.ToEmailAddress, email.TemplateId, email.Tags ?? ["(none)"]);

        return Result.Ok;
    }
}