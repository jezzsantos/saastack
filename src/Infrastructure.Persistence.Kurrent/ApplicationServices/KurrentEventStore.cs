using Common;
using Common.Configuration;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Interfaces;

namespace Infrastructure.Persistence.Kurrent.ApplicationServices;

/// <summary>
///     Provides an event store to Kurrent
/// </summary>
public class KurrentEventStore : IEventStore
{
    public static KurrentEventStore Create(IRecorder recorder, IConfigurationSettings settings)
    {
        //TODO: read from settings
        return new KurrentEventStore();
    }

    public async Task<Result<string, Error>> AddEventsAsync(string entityName, string entityId,
        List<EventSourcedChangeEvent> events, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

#if TESTINGONLY
    async Task<Result<Error>> IEventStore.DestroyAllAsync(string entityName, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }
#endif

    public async Task<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>> GetEventStreamAsync(string entityName,
        string entityId, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}