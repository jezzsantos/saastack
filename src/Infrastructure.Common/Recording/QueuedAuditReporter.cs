#if !TESTINGONLY
using Infrastructure.Persistence.Common.ApplicationServices;
#if HOSTEDONAZURE
using Infrastructure.External.Persistence.Azure.ApplicationServices;
#elif HOSTEDONAWS
using Infrastructure.External.Persistence.AWS.ApplicationServices;
#endif
#else
using Infrastructure.Persistence.Interfaces;
#endif
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces;
using Domain.Interfaces.Services;
using Infrastructure.Persistence.Shared.ApplicationServices;

namespace Infrastructure.Common.Recording;

/// <summary>
///     Provides an <see cref="IAuditReporter" /> that asynchronously brokers the audit to a reliable queue for future
///     delivery
/// </summary>
public class QueuedAuditReporter : IAuditReporter
{
    private readonly IAuditMessageQueueRepository _repository;
    private readonly IHostRegionService _hostRegionService;

    // ReSharper disable once UnusedParameter.Local
    public QueuedAuditReporter(IDependencyContainer container, IConfigurationSettings settings,
        IHostRegionService hostRegionService)
        : this(new AuditMessageQueueRepository(NoOpRecorder.Instance,
            container.GetRequiredService<IHostRegionService>(),
            container.GetRequiredService<IMessageQueueMessageIdFactory>(),
#if !TESTINGONLY
#if HOSTEDONAZURE
            AzureStorageAccountQueueStore.Create(NoOpRecorder.Instance, AzureStorageAccountStoreOptions.Credentials(settings))
#elif HOSTEDONAWS
            AWSSQSQueueStore.Create(NoOpRecorder.Instance, settings)
#endif
#else
            container.GetRequiredServiceForPlatform<IQueueStore>()
#endif
        ), hostRegionService)
    {
    }

    internal QueuedAuditReporter(IAuditMessageQueueRepository repository, IHostRegionService hostRegionService)
    {
        _repository = repository;
        _hostRegionService = hostRegionService;
    }

    public void Audit(ICallContext? call, string againstId, string auditCode, string messageTemplate,
        params object[] templateArgs)
    {
        ArgumentException.ThrowIfNullOrEmpty(againstId);
        ArgumentException.ThrowIfNullOrEmpty(auditCode);

        var region = _hostRegionService.GetRegion();
        var safeCall = call ?? CallContext.CreateUnknown(region);
        var message = new AuditMessage
        {
            AuditCode = auditCode,
            AgainstId = againstId,
            MessageTemplate = messageTemplate,
            Arguments = templateArgs.HasAny()
                ? templateArgs.Select(arg => arg.ToString()!)
                    .ToList()
                : new List<string>()
        };

        _repository.PushAsync(safeCall, message, CancellationToken.None).GetAwaiter().GetResult();
    }
}