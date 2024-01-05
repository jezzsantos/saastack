using Application.Common;
using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Services.Shared;
using Common;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a queueing service for asynchronous delivery of emails
/// </summary>
public class EmailSendingService : IEmailSendingService
{
    private readonly IRecorder _recorder;
    private readonly IEmailMessageQueueRepository _repository;

    public EmailSendingService(IRecorder recorder, IEmailMessageQueueRepository repository)
    {
        _recorder = recorder;
        _repository = repository;
    }

    public async Task<Result<Error>> SendHtmlEmail(ICallerContext caller, HtmlEmail htmlEmail,
        CancellationToken cancellationToken)
    {
        var queued = await _repository.PushAsync(caller.ToCall(), new EmailMessage
        {
            Html = new QueuedEmailHtmlMessage
            {
                Subject = htmlEmail.Subject,
                FromEmail = htmlEmail.FromEmailAddress,
                FromDisplayName = htmlEmail.FromDisplayName,
                HtmlBody = htmlEmail.Body,
                ToEmail = htmlEmail.ToEmailAddress,
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