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
///     Provides an <see cref="IAuditReporter" /> that asynchronously brokers the audit to a reliable queue for future
///     delivery
/// </summary>
public class QueuedAuditReporter : IAuditReporter
{
    private readonly IAuditMessageQueueRepository _repository;

    // ReSharper disable once UnusedParameter.Local
    public QueuedAuditReporter(IDependencyContainer container, IConfigurationSettings settings)
        : this(new AuditMessageQueueRepository(NoOpRecorder.Instance,
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

    internal QueuedAuditReporter(IAuditMessageQueueRepository repository)
    {
        _repository = repository;
    }

    public void Audit(ICallContext? context, string againstId, string auditCode, string messageTemplate,
        params object[] templateArgs)
    {
        ArgumentException.ThrowIfNullOrEmpty(againstId);
        ArgumentException.ThrowIfNullOrEmpty(auditCode);

        var call = context ?? CallContext.CreateUnknown();
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

        _repository.PushAsync(call, message, CancellationToken.None).GetAwaiter().GetResult();
    }
}