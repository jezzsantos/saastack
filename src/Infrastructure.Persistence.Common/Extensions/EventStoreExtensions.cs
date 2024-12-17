using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Infrastructure.Persistence.Interfaces;

namespace Infrastructure.Persistence.Common.Extensions;

public static class EventStoreExtensions
{
    /// <summary>
    ///     Verifies that the version of the latest event produced by the aggregate is the next event in the stream of events
    ///     from the store.
    ///     In other words, that the event stream of the aggregate in the store has not been updated while the
    ///     aggregate has been changed in memory.
    /// </summary>
    public static Result<Error> VerifyConcurrencyCheck(this IEventStore eventStore, string streamName,
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

        if (nextEventVersion <= latestStoredEventVersion)
        {
            return Error.EntityExists(
                Resources.EventStore_ConcurrencyVerificationFailed_StreamAlreadyUpdated.Format(streamName,
                    nextEventVersion));
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