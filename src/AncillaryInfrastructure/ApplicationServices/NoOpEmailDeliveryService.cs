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

    public Task<Result<EmailDeliveryReceipt, Error>> DeliverAsync(ICallerContext context, string subject,
        string htmlBody,
        string toEmailAddress, string? toDisplayName,
        string fromEmailAddress, string? fromDisplayName, CancellationToken cancellationToken = default)
    {
        _recorder.TraceInformation(context.ToCall(),
            $"{nameof(NoOpUsageDeliveryService)} delivers email event {{Event}} for {{For}} with properties: {{Properties}}",
            subject, toEmailAddress, new
            {
                To = toEmailAddress,
                ToDisplayName = toDisplayName,
                From = fromEmailAddress,
                FromDisplayName = fromDisplayName,
                Body = htmlBody
            }.ToJson()!);

        return Task.FromResult<Result<EmailDeliveryReceipt, Error>>(new EmailDeliveryReceipt());
    }
}