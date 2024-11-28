using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Common;
using Common.Extensions;
using Task = System.Threading.Tasks.Task;

namespace AncillaryInfrastructure.ApplicationServices;

/// <summary>
///     Provides a <see cref="IUsageDeliveryService" /> that does nothing
/// </summary>
public class NoOpUsageDeliveryService : IUsageDeliveryService
{
    private readonly IRecorder _recorder;

    public NoOpUsageDeliveryService(IRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task<Result<Error>> DeliverAsync(ICallerContext caller, string forId, string eventName,
        Dictionary<string, string>? additional = null,
        CancellationToken cancellationToken = default)
    {
        var properties = additional.Exists()
            ? additional.ToJson(false)!
            : "none";
        _recorder.TraceInformation(caller.ToCall(),
            $"{nameof(NoOpUsageDeliveryService)} would have delivered usage event: {{Event}}, for: {{For}}, with properties: {{Properties}}",
            eventName, forId, properties);

        return Task.FromResult(Result.Ok);
    }
}