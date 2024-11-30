using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Common;
using Common.Extensions;
using Task = System.Threading.Tasks.Task;

namespace AncillaryInfrastructure.ApplicationServices;

/// <summary>
///     Provides a <see cref="IEmailDeliveryService" /> that does nothing
/// </summary>
public class NoOpEmailDeliveryService : IEmailDeliveryService
{
    private readonly IRecorder _recorder;

    public NoOpEmailDeliveryService(IRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task<Result<EmailDeliveryReceipt, Error>> SendHtmlAsync(ICallerContext caller, string subject,
        string htmlBody,
        string toEmailAddress, string? toDisplayName, string fromEmailAddress, string? fromDisplayName,
        CancellationToken cancellationToken)
    {
        _recorder.TraceInformation(caller.ToCall(),
            $"{nameof(NoOpEmailDeliveryService)} would have delivered HTML email message {{To}}, from {{From}}, with subject {{Subject}}, body {{Body}}",
            toEmailAddress, fromEmailAddress, subject, htmlBody);

        return Task.FromResult<Result<EmailDeliveryReceipt, Error>>(new EmailDeliveryReceipt
        {
            ReceiptId = $"receipt_{Guid.NewGuid():N}"
        });
    }

    public Task<Result<EmailDeliveryReceipt, Error>> SendTemplatedAsync(ICallerContext caller, string templateId,
        string? subject,
        Dictionary<string, string> substitutions, string toEmailAddress,
        string? toDisplayName, string fromEmailAddress, string? fromDisplayName, CancellationToken cancellationToken)
    {
        _recorder.TraceInformation(caller.ToCall(),
            $"{nameof(NoOpEmailDeliveryService)} would have delivered Templated email message {{To}}, from {{From}}, with template {{Template}}, and substitutions {{Substitutions}}",
            toEmailAddress, fromEmailAddress, templateId, substitutions.Exists()
                ? substitutions.ToJson()!
                : "none");

        return Task.FromResult<Result<EmailDeliveryReceipt, Error>>(new EmailDeliveryReceipt
        {
            ReceiptId = $"receipt_{Guid.NewGuid():N}"
        });
    }
}