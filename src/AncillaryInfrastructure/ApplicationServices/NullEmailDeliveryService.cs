using Application.Common;
using Application.Interfaces;
using Application.Persistence.Shared;
using Common;
using Common.Extensions;
using Task = System.Threading.Tasks.Task;

namespace AncillaryInfrastructure.ApplicationServices;

/// <summary>
///     Provides a <see cref="IEmailDeliveryService" /> that does nothing
/// </summary>
public class NullEmailDeliveryService : IEmailDeliveryService
{
    private readonly IRecorder _recorder;

    public NullEmailDeliveryService(IRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task<Result<Error>> DeliverAsync(ICallerContext context, string subject, string htmlBody,
        string toEmailAddress, string? toDisplayName,
        string fromEmailAddress, string? fromDisplayName, CancellationToken cancellationToken = default)
    {
        _recorder.TraceInformation(context.ToCall(),
            $"{nameof(NullUsageDeliveryService)} delivers email event {{Event}} for {{For}} with properties: {{Properties}}",
            subject, toEmailAddress, new
            {
                To = toEmailAddress,
                ToDisplayName = toDisplayName,
                From = fromEmailAddress,
                FromDisplayName = fromDisplayName,
                Body = htmlBody
            }.ToJson()!);

        return Task.FromResult(Result.Ok);
    }
}