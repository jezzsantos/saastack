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
        IHttpClientFactory httpClientFactory) : this(recorder,
        new MailgunClient(recorder, settings, httpClientFactory))
    {
    }

    public MailgunHttpServiceClient(IRecorder recorder, IMailgunClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
    }

    public async Task<Result<EmailDeliveryReceipt, Error>> SendAsync(ICallerContext caller, string subject,
        string htmlBody, string toEmailAddress, string? toDisplayName,
        string fromEmailAddress, string? fromDisplayName, CancellationToken cancellationToken = default)
    {
        _recorder.TraceInformation(caller.ToCall(), "Sending email to {To} in Mailgun from {From}", toEmailAddress,
            fromEmailAddress);

        var sent = await _serviceClient.SendAsync(caller.ToCall(), subject, fromEmailAddress, fromDisplayName,
            new MailGunRecipient
            {
                DisplayName = toDisplayName ?? string.Empty,
                EmailAddress = toEmailAddress
            },
            htmlBody, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Sent email to {To} in Mailgun, from {From} successfully",
            toEmailAddress, fromEmailAddress);

        return sent.Value;
    }
}