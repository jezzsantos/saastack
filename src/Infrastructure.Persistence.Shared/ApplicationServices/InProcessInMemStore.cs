#if TESTINGONLY
using System.Diagnostics.CodeAnalysis;
using Common;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;

namespace Infrastructure.Persistence.Shared.ApplicationServices;

/// <summary>
///     Provides a combined store that persists all data to memory, in the current process
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