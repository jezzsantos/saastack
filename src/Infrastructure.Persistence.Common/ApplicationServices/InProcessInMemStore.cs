#if TESTINGONLY
using System.Diagnostics.CodeAnalysis;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;

namespace Infrastructure.Persistence.Common.ApplicationServices;

/// <summary>
///     Provides a combined store that persists all data to memory, in the current process
///     Note: Should NEVER be used in production systems, and useful only in the current process.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class InProcessInMemStore
{
    public static InProcessInMemStore Create(IMessageMonitor? monitor = default)
    {
        return new InProcessInMemStore(monitor);
    }

    private InProcessInMemStore(IMessageMonitor? monitor = default)
    {
        if (monitor.Exists())
        {
            FireQueueMessage += (_, args) => { monitor.NotifyQueueMessagesChanged(args.QueueName, args.MessageCount); };
            NotifyPendingQueuedMessages();
            FireTopicMessage += (_, args) => { monitor.NotifyTopicMessagesChanged(args.QueueName, args.MessageCount); };
            NotifyPendingBusTopicMessages();
        }
    }
}

internal static class InProcessInMemDataStoreExtensions
{
    public static HydrationProperties ToHydrationProperties(this CommandEntity entity)
    {
        foreach (var (propertyName, propertyValue) in entity.Properties)
        {
            if (!propertyValue.HasValue)
            {
                continue;
            }

            var value = propertyValue.Value;
            switch (value)
            {
                case DateTime dateTime:
                {
                    if (dateTime == DateTime.MinValue)
                    {
                        entity.Add(propertyName, DateTime.MinValue.ToUniversalTime());
                    }

                    break;
                }

                case DateTimeOffset dateTimeOffset:
                {
                    if (dateTimeOffset == DateTimeOffset.MinValue)
                    {
                        entity.Add(propertyName, DateTimeOffset.MinValue.ToUniversalTime());
                    }

                    break;
                }
            }
        }

        entity.LastPersistedAtUtc = DateTime.UtcNow;

        return entity.Properties;
    }
}
#endif