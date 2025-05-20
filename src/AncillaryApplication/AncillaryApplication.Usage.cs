using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared.Extensions;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;

namespace AncillaryApplication;

partial class AncillaryApplication
{
    public async Task<Result<bool, Error>> DeliverUsageAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken)
    {
        var rehydrated = messageAsJson.RehydrateQueuedMessage<UsageMessage>();
        if (rehydrated.IsFailure)
        {
            return rehydrated.Error;
        }

        var delivered = await DeliverUsageInternalAsync(caller, rehydrated.Value, cancellationToken);
        if (delivered.IsFailure)
        {
            return delivered.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Delivered usage message: {Message}", messageAsJson);
        return true;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DrainAllUsagesAsync(ICallerContext caller, CancellationToken cancellationToken)
    {
        await _usageMessageQueue.DrainAllQueuedMessagesAsync(
            message => DeliverUsageInternalAsync(caller, message, cancellationToken), cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Drained all usage messages");

        return Result.Ok;
    }
#endif

    private async Task<Result<bool, Error>> DeliverUsageInternalAsync(ICallerContext caller, UsageMessage message,
        CancellationToken cancellationToken)
    {
        if (message.ForId.IsInvalidParameter(x => x.HasValue(), nameof(UsageMessage.ForId), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_Usage_MissingForId);
        }

        if (message.EventName.IsInvalidParameter(x => x.HasValue(), nameof(UsageMessage.EventName), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_Usage_MissingEventName);
        }

        var region = message.OriginHostRegion ?? DatacenterLocations.Unknown.Code;
        var additional = message.Additional ?? new Dictionary<string, string>();
        additional.TryAdd(UsageConstants.Properties.Region, region);

        var delivered = await _usageDeliveryService.DeliverAsync(caller, message.ForId!, message.EventName!,
            additional, cancellationToken);
        if (delivered.IsFailure)
        {
            return delivered.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Delivered usage for {For} (from {Region})", message.ForId!,
            message.OriginHostRegion ?? DatacenterLocations.Unknown.Code);

        return true;
    }
}