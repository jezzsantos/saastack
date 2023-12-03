#if TESTINGONLY
using System.Diagnostics.CodeAnalysis;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;

namespace Infrastructure.Persistence.Common.ApplicationServices;

/// <summary>
///     Defines a combined store that persists all data to memory, in the current process
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class InProcessInMemStore
{
    public InProcessInMemStore(Optional<IQueueStoreNotificationHandler> handler = default)
    {
        if (handler.HasValue)
        {
            FireMessageQueueUpdated += (_, args) =>
            {
                handler.Value.HandleMessagesQueueUpdated(args.QueueName, args.MessageCount);
            };
            NotifyAllQueuedMessages();
        }
    }

    public async Task<Result<Error>> DestroyAllAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

#if TESTINGONLY
        await ((IDataStore)this).DestroyAllAsync(containerName, cancellationToken);
        await ((IBlobStore)this).DestroyAllAsync(containerName, cancellationToken);
        await ((IQueueStore)this).DestroyAllAsync(containerName, cancellationToken);
        await ((IEventStore)this).DestroyAllAsync(containerName, cancellationToken);
#endif

        return Result.Ok;
    }
}

internal static class InProcessInMemDataStoreExtensions
{
    public static HydrationProperties ToHydrationProperties(this CommandEntity entity)
    {
        entity.LastPersistedAtUtc = DateTime.UtcNow;
        return entity.Properties;
    }
}
#endif