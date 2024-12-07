using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Common;
using Common.Configuration;

namespace Infrastructure.Shared.ApplicationServices.External;

/// <summary>
///     Provides an adapter to the Mailgun.com service
///     <see href="https://documentation.mailgun.com/docs/mailgun/api-reference/intro/" />
/// </summary>
public class MailgunHttpServiceClient : IEmailDeliveryService
{
    private readonly IRecorder _recorder;
    private readonly IMailgunClient _serviceClient;

    public MailgunHttpServiceClient(IRecorder recorder, IConfigurationSettings settings,
        IHttpClientFactory httpClientFactory) : this(recorder, new MailgunClient(recorder, settings, httpClientFactory))
    {
    }

    public MailgunHttpServiceClient(IRecorder recorder, IMailgunClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
    }

    public async Task<Result<EmailDeliveryReceipt, Error>> SendHtmlAsync(ICallerContext caller, string subject,
        string htmlBody, string toEmailAddress, string? toDisplayName,
        string fromEmailAddress, string? fromDisplayName, IReadOnlyList<string>? tags,
        CancellationToken cancellationToken)
    {
        _recorder.TraceInformation(caller.ToCall(), "Sending HTML email to {To} in Mailgun from {From}", toEmailAddress,
            fromEmailAddress);

        var sent = await _serviceClient.SendHtmlAsync(caller.ToCall(), subject, fromEmailAddress, fromDisplayName,
            new MailGunRecipient
            {
                DisplayName = toDisplayName ?? string.Empty,
                EmailAddress = toEmailAddress
            },
            htmlBody, tags, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Sent HTML email to {To} in Mailgun, from {From} successfully",
            toEmailAddress, fromEmailAddress);

        return sent.Value;
    }

    public async Task<Result<EmailDeliveryReceipt, Error>> SendTemplatedAsync(ICallerContext caller, string templateId,
        string? subject, Dictionary<string, string> substitutions, string toEmailAddress,
        string? toDisplayName, string fromEmailAddress, string? fromDisplayName, IReadOnlyList<string>? tags,
        CancellationToken cancellationToken)
    {
        _recorder.TraceInformation(caller.ToCall(), "Sending templated email to {To} in Mailgun from {From}",
            toEmailAddress,
            fromEmailAddress);

        var sent = await _serviceClient.SendTemplatedAsync(caller.ToCall(), templateId, subject, fromEmailAddress,
            fromDisplayName, new MailGunRecipient
            {
                DisplayName = toDisplayName ?? string.Empty,
                EmailAddress = toEmailAddress
            },
            substitutions, tags, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Sent templated email to {To} in Mailgun, from {From} successfully",
            toEmailAddress, fromEmailAddress);

        return sent.Value;
    }
}