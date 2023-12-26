#if !TESTINGONLY
using Infrastructure.Persistence.Common.ApplicationServices;
#if HOSTEDONAZURE
using Infrastructure.Persistence.Azure.ApplicationServices;
#elif HOSTEDONAWS
#endif
#else
using Infrastructure.Persistence.Interfaces;
#endif
using Application.Interfaces;
using Application.Persistence.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces.Services;
using Infrastructure.Persistence.Shared.ApplicationServices;

namespace Infrastructure.Common.Recording;

/// <summary>
///     Provides an <see cref="IUsageReporter" /> that asynchronously brokers the audit to a reliable queue for future
///     delivery
/// </summary>
public class QueuedUsageReporter : IUsageReporter
{
    private readonly IUsageMessageQueueRepository _repository;

    // ReSharper disable once UnusedParameter.Local
    public QueuedUsageReporter(IDependencyContainer container, ISettings settings)
        : this(new UsageMessageQueueRepository(NullRecorder.Instance,
#if !TESTINGONLY
#if HOSTEDONAZURE
                AzureStorageAccountQueueStore.Create(NullRecorder.Instance, settings)
#elif HOSTEDONAWS
                NullStore.Instance
#endif
#else
            container.Resolve<IQueueStore>()
#endif
        ))
    {
    }

    internal QueuedUsageReporter(IUsageMessageQueueRepository repository)
    {
        _repository = repository;
    }

    public void Track(ICallContext? context, string forId, string eventName,
        Dictionary<string, object>? additional = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(forId);
        ArgumentException.ThrowIfNullOrEmpty(eventName);

        var call = context ?? CallContext.CreateUnknown();
        var properties = additional ?? new Dictionary<string, object>();

        properties[UsageConstants.Properties.CallId] = call.CallId;
        properties[UsageConstants.Properties.TenantId] = call.TenantId!;

        var message = new UsageMessage
        {
            EventName = eventName,
            ForId = forId,
            Additional = properties.ToDictionary(pair => pair.Key, pair => pair.Value.Exists()
                ? pair.Value.ToString() ?? string.Empty
                : string.Empty)
        };

        _repository.PushAsync(call, message, CancellationToken.None).GetAwaiter().GetResult();
    }
}