#if !TESTINGONLY
using Infrastructure.Persistence.Common.ApplicationServices;
#if HOSTEDONPREMISES
using Infrastructure.Persistence.OnPremises.ApplicationServices;
#elif HOSTEDONAZURE
using Infrastructure.Persistence.Azure.ApplicationServices;
#elif HOSTEDONAWS
using Infrastructure.Persistence.AWS.ApplicationServices;
#endif
#else
using Infrastructure.Persistence.Interfaces;
#endif
using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces;
using Domain.Interfaces.Services;
using Infrastructure.Persistence.Shared.ApplicationServices;

namespace Infrastructure.Common.Recording;

/// <summary>
///     Provides an <see cref="IUsageReporter" /> that asynchronously brokers the audit to a reliable queue for future
///     delivery
/// </summary>
public class QueuedUsageReporter : IUsageReporter
{
    private readonly IUsageMessageQueue _queue;

    // ReSharper disable once UnusedParameter.Local
    public QueuedUsageReporter(IDependencyContainer container, IConfigurationSettings settings)
        : this(new UsageMessageQueue(NoOpRecorder.Instance,
            container.GetRequiredService<IMessageQueueMessageIdFactory>(),
#if !TESTINGONLY
#if HOSTEDONPREMISES
            RabbitMqQueueStore.Create(NoOpRecorder.Instance, RabbitMqStoreOptions.FromCredentials(settings))
#elif HOSTEDONAZURE
            AzureStorageAccountQueueStore.Create(NoOpRecorder.Instance, AzureStorageAccountStoreOptions.Credentials(settings))
#elif HOSTEDONAWS
            AWSSQSQueueStore.Create(NoOpRecorder.Instance, settings)
#endif
#else
            container.GetRequiredServiceForPlatform<IQueueStore>()
#endif
        ))
    {
    }

    internal QueuedUsageReporter(IUsageMessageQueue queue)
    {
        _queue = queue;
    }

    public async Task<Result<Error>> TrackAsync(ICallContext? call, string forId, string eventName,
        Dictionary<string, object>? additional = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(forId);
        ArgumentException.ThrowIfNullOrEmpty(eventName);

        var safeCall = call ?? CallContext.CreateUnknown();
        var properties = additional ?? new Dictionary<string, object>();

        properties[UsageConstants.Properties.CallId] = safeCall.CallId;
        properties[UsageConstants.Properties.TenantId] = safeCall.TenantId!;

        var message = new UsageMessage
        {
            EventName = eventName,
            ForId = forId,
            Additional = properties.ToDictionary(pair => pair.Key, pair => pair.Value.Exists()
                ? pair.Value.ToString() ?? string.Empty
                : string.Empty)
        };

        var queued = await _queue.PushAsync(safeCall, message, cancellationToken);
        if (queued.IsFailure)
        {
            return queued.Error;
        }

        return Result.Ok;
    }
}