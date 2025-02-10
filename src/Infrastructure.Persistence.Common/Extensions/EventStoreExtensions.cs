using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Infrastructure.Persistence.Interfaces;

namespace Infrastructure.Persistence.Common.Extensions;

public static class EventStoreExtensions
{
    /// <summary>
    ///     Verifies that the version of the latest event produced by the aggregate is the next event in the stream of events
    ///     from the store, with no version gaps between them. IN other words, they are contiguous
    /// </summary>
    public static Result<Error> VerifyContiguousCheck(this IEventStore eventStore, string streamName,
        Optional<int> latestStoredEventVersion, int nextEventVersion)
    {
        if (!latestStoredEventVersion.HasValue)
        {
            if (nextEventVersion != EventStream.FirstVersion)
            {
                return Error.EntityExists(
                    Resources.EventStore_ConcurrencyVerificationFailed_StreamReset.Format(streamName));
            }

            return Result.Ok;
        }

        var expectedNextVersion = latestStoredEventVersion + 1;
        if (nextEventVersion > expectedNextVersion)
        {
            return Error.EntityExists(
                Resources.EventStore_ConcurrencyVerificationFailed_MissingUpdates.Format(streamName,
                    expectedNextVersion, nextEventVersion));
        }

        return Result.Ok;
    }
}