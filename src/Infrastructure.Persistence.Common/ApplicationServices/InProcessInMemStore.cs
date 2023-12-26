#if TESTINGONLY
using System.Diagnostics.CodeAnalysis;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;

namespace Infrastructure.Persistence.Common.ApplicationServices;

/// <summary>
///     Provides a combined store that persists all data to memory, in the current process
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class InProcessInMemStore
{
    public static InProcessInMemStore Create(IQueueStoreNotificationHandler? handler = default)
    {
        return new InProcessInMemStore(handler);
    }

    private InProcessInMemStore(IQueueStoreNotificationHandler? handler = default)
    {
        if (handler.Exists())
        {
            FireMessageQueueUpdated += (_, args) =>
            {
                handler.HandleMessagesQueueUpdated(args.QueueName, args.MessageCount);
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