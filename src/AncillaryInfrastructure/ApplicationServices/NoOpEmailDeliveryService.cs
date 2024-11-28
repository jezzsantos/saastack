using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Common;
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

    public Task<Result<EmailDeliveryReceipt, Error>> SendAsync(ICallerContext caller, string subject, string htmlBody,
        string toEmailAddress, string? toDisplayName, string fromEmailAddress, string? fromDisplayName,
        CancellationToken cancellationToken)
    {
        _recorder.TraceInformation(caller.ToCall(),
            $"{nameof(NoOpEmailDeliveryService)} would have delivered email message {{To}}, from {{From}}, with subject {{Subject}}, body {{Body}}",
            toEmailAddress, fromEmailAddress, subject, htmlBody);

        return Task.FromResult<Result<EmailDeliveryReceipt, Error>>(new EmailDeliveryReceipt
        {
            ReceiptId = $"receipt_{Guid.NewGuid():N}"
        });
    }
}