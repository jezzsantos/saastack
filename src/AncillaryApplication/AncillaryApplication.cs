using System.Text.Json;
using AncillaryApplication.Persistence;
using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Shared;
using Audit = Application.Resources.Shared.Audit;

namespace AncillaryApplication;

public class AncillaryApplication : IAncillaryApplication
{
    private readonly IAuditRepository _auditRepository;
    private readonly IEmailDeliveryRepository _emailDeliveryRepository;
    private readonly IEmailDeliveryService _emailDeliveryService;
    private readonly IIdentifierFactory _idFactory;
    private readonly IRecorder _recorder;
    private readonly IUsageDeliveryService _usageDeliveryService;
    private readonly IProvisioningNotificationService _provisioningNotificationService;
#if TESTINGONLY
    private readonly IAuditMessageQueueRepository _auditMessageQueueRepository;
    private readonly IEmailMessageQueue _emailMessageQueue;
    private readonly IUsageMessageQueue _usageMessageQueue;
    private readonly IProvisioningMessageQueue _provisioningMessageQueue;

    public AncillaryApplication(IRecorder recorder, IIdentifierFactory idFactory,
        IUsageMessageQueue usageMessageQueue, IUsageDeliveryService usageDeliveryService,
        IAuditMessageQueueRepository auditMessageQueueRepository, IAuditRepository auditRepository,
        IEmailMessageQueue emailMessageQueue, IEmailDeliveryService emailDeliveryService,
        IEmailDeliveryRepository emailDeliveryRepository,
        IProvisioningMessageQueue provisioningMessageQueue,
        IProvisioningNotificationService provisioningNotificationService)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _usageMessageQueue = usageMessageQueue;
        _usageDeliveryService = usageDeliveryService;
        _auditMessageQueueRepository = auditMessageQueueRepository;
        _auditRepository = auditRepository;
        _emailMessageQueue = emailMessageQueue;
        _emailDeliveryService = emailDeliveryService;
        _emailDeliveryRepository = emailDeliveryRepository;
        _provisioningMessageQueue = provisioningMessageQueue;
        _provisioningNotificationService = provisioningNotificationService;
    }
#else
    public AncillaryApplication(IRecorder recorder, IIdentifierFactory idFactory,
        // ReSharper disable once UnusedParameter.Local
        IUsageMessageQueue usageMessageQueue, IUsageDeliveryService usageDeliveryService,
        // ReSharper disable once UnusedParameter.Local
        IAuditMessageQueueRepository auditMessageQueueRepository, IAuditRepository auditRepository,
        // ReSharper disable once UnusedParameter.Local
        IEmailMessageQueue emailMessageQueue, IEmailDeliveryService emailDeliveryService,
        IEmailDeliveryRepository emailDeliveryRepository,
        // ReSharper disable once UnusedParameter.Local
        IProvisioningMessageQueue provisioningMessageQueue,
        IProvisioningNotificationService provisioningNotificationService)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _usageDeliveryService = usageDeliveryService;
        _auditRepository = auditRepository;
        _emailDeliveryService = emailDeliveryService;
        _emailDeliveryRepository = emailDeliveryRepository;
        _provisioningNotificationService = provisioningNotificationService;
    }
#endif

    public async Task<Result<bool, Error>> DeliverEmailAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken)
    {
        var rehydrated = RehydrateMessage<EmailMessage>(messageAsJson);
        if (rehydrated.IsFailure)
        {
            return rehydrated.Error;
        }

        var delivered = await DeliverEmailInternalAsync(caller, rehydrated.Value, cancellationToken);
        if (delivered.IsFailure)
        {
            return delivered.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Delivered email message: {Message}", messageAsJson);
        return true;
    }

    public async Task<Result<bool, Error>> NotifyProvisioningAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken)
    {
        var rehydrated = RehydrateMessage<ProvisioningMessage>(messageAsJson);
        if (rehydrated.IsFailure)
        {
            return rehydrated.Error;
        }

        var delivered = await NotifyProvisioningInternalAsync(caller, rehydrated.Value, cancellationToken);
        if (delivered.IsFailure)
        {
            return delivered.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Delivered provisioning message: {Message}", messageAsJson);
        return true;
    }

    public async Task<Result<bool, Error>> DeliverUsageAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken)
    {
        var rehydrated = RehydrateMessage<UsageMessage>(messageAsJson);
        if (rehydrated.IsFailure)
        {
            return rehydrated.Error;
        }

        var delivered = await DeliverUsageInternalAsync(caller, rehydrated.Value, cancellationToken);
        if (delivered.IsFailure)
        {
            return delivered.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Delivered usage message: {Message}", messageAsJson);
        return true;
    }

    public async Task<Result<bool, Error>> DeliverAuditAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken)
    {
        var rehydrated = RehydrateMessage<AuditMessage>(messageAsJson);
        if (rehydrated.IsFailure)
        {
            return rehydrated.Error;
        }

        var delivered = await DeliverAuditInternalAsync(caller, rehydrated.Value, cancellationToken);
        if (delivered.IsFailure)
        {
            return delivered.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Delivered audit message: {Message}", messageAsJson);
        return true;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DrainAllEmailsAsync(ICallerContext caller, CancellationToken cancellationToken)
    {
        await DrainAllOnQueueAsync(_emailMessageQueue,
            message => DeliverEmailInternalAsync(caller, message, cancellationToken), cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Drained all email messages");

        return Result.Ok;
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DrainAllProvisioningsAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        await DrainAllOnQueueAsync(_provisioningMessageQueue,
            message => NotifyProvisioningInternalAsync(caller, message, cancellationToken), cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Drained all provisioning messages");

        return Result.Ok;
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DrainAllUsagesAsync(ICallerContext caller, CancellationToken cancellationToken)
    {
        await DrainAllOnQueueAsync(_usageMessageQueue,
            message => DeliverUsageInternalAsync(caller, message, cancellationToken), cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Drained all usage messages");

        return Result.Ok;
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DrainAllAuditsAsync(ICallerContext caller, CancellationToken cancellationToken)
    {
        await DrainAllOnQueueAsync(_auditMessageQueueRepository,
            message => DeliverAuditInternalAsync(caller, message, cancellationToken), cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Drained all audit messages");

        return Result.Ok;
    }
#endif

#if TESTINGONLY
    public async Task<Result<SearchResults<Audit>, Error>> SearchAllAuditsAsync(ICallerContext caller,
        string organizationId, SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken)
    {
        var searched = await _auditRepository.SearchAllAsync(organizationId.ToId(), searchOptions, cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        var audits = searched.Value;

        return searchOptions.ApplyWithMetadata(audits.Select(audit => audit.ToAudit()));
    }
#endif

    public async Task<Result<SearchResults<DeliveredEmail>, Error>> SearchAllEmailDeliveriesAsync(
        ICallerContext caller, DateTime? sinceUtc, SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken)
    {
        var sinceWhen = sinceUtc ?? DateTime.UtcNow.SubtractDays(14);
        var searched =
            await _emailDeliveryRepository.SearchAllDeliveriesAsync(sinceWhen, searchOptions, cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        var deliveries = searched.Value;
        _recorder.TraceInformation(caller.ToCall(), "All email deliveries since {Since} were fetched",
            sinceUtc.ToIso8601());

        return searchOptions.ApplyWithMetadata(
            deliveries.Select(delivery => delivery.ToDeliveredEmail()));
    }

    private async Task<Result<bool, Error>> DeliverEmailInternalAsync(ICallerContext caller, EmailMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Html.IsInvalidParameter(x => x.Exists(), nameof(EmailMessage.Html), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_Email_MissingHtml);
        }

        var messageId = QueuedMessageId.Create(message.MessageId!);
        if (messageId.IsFailure)
        {
            return messageId.Error;
        }

        var retrieved = await _emailDeliveryRepository.FindDeliveryByMessageIdAsync(messageId.Value, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subject = message.Html!.Subject;
        var body = message.Html!.HtmlBody;
        var recipientEmailAddress = EmailAddress.Create(message.Html!.ToEmailAddress!);
        if (recipientEmailAddress.IsFailure)
        {
            return recipientEmailAddress.Error;
        }

        var recipientName = message.Html!.ToDisplayName ?? string.Empty;
        var recipient = EmailRecipient.Create(recipientEmailAddress.Value, recipientName);
        if (recipient.IsFailure)
        {
            return recipient.Error;
        }

        var senderEmailAddress = EmailAddress.Create(message.Html!.FromEmailAddress!);
        if (senderEmailAddress.IsFailure)
        {
            return senderEmailAddress.Error;
        }

        var senderName = message.Html!.FromDisplayName ?? string.Empty;
        var sender = EmailRecipient.Create(senderEmailAddress.Value, senderName);
        if (sender.IsFailure)
        {
            return sender.Error;
        }

        EmailDeliveryRoot delivery;
        var found = retrieved.Value.HasValue;
        if (found)
        {
            delivery = retrieved.Value.Value;
        }
        else
        {
            var created = EmailDeliveryRoot.Create(_recorder, _idFactory, messageId.Value);
            if (created.IsFailure)
            {
                return created.Error;
            }

            delivery = created.Value;

            var detailed = delivery.SetEmailDetails(subject, body, recipient.Value);
            if (detailed.IsFailure)
            {
                return detailed.Error;
            }
        }

        var makeAttempt = delivery.AttemptDelivery();
        if (makeAttempt.IsFailure)
        {
            return makeAttempt.Error;
        }

        var isAlreadyDelivered = makeAttempt.Value;
        if (isAlreadyDelivered)
        {
            _recorder.TraceInformation(caller.ToCall(), "Email for {For} is already delivered",
                delivery.Recipient.Value.EmailAddress.Address);
            return true;
        }

        var saved = await _emailDeliveryRepository.SaveAsync(delivery, true, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        var deliveryBefore = saved.Value;
        var delivered = await _emailDeliveryService.DeliverAsync(caller, subject!, body!,
            recipient.Value.EmailAddress, recipient.Value.DisplayName, sender.Value.EmailAddress,
            sender.Value.DisplayName,
            cancellationToken);
        if (delivered.IsFailure)
        {
            var failed = deliveryBefore.FailedDelivery();
            if (failed.IsFailure)
            {
                return failed.Error;
            }

            var savedFailure = await _emailDeliveryRepository.SaveAsync(deliveryBefore, false, cancellationToken);
            if (savedFailure.IsFailure)
            {
                return savedFailure.Error;
            }

            _recorder.TraceInformation(caller.ToCall(), "Delivery of email for {For}, failed",
                savedFailure.Value.Recipient.Value.EmailAddress.Address);

            return delivered.Error;
        }

        var succeeded = deliveryBefore.SucceededDelivery(delivered.Value.TransactionId.ToOptional());
        if (succeeded.IsFailure)
        {
            return succeeded.Error;
        }

        var updated = await _emailDeliveryRepository.SaveAsync(deliveryBefore, false, cancellationToken);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        deliveryBefore = updated.Value;
        _recorder.TraceInformation(caller.ToCall(), "Delivered email for {For}",
            deliveryBefore.Recipient.Value.EmailAddress.Address);

        return true;
    }

    private async Task<Result<bool, Error>> DeliverUsageInternalAsync(ICallerContext caller, UsageMessage message,
        CancellationToken cancellationToken)
    {
        if (message.ForId.IsInvalidParameter(x => x.HasValue(), nameof(UsageMessage.ForId), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_Usage_MissingForId);
        }

        if (message.EventName.IsInvalidParameter(x => x.HasValue(), nameof(UsageMessage.EventName), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_Usage_MissingEventName);
        }

        var delivered = await _usageDeliveryService.DeliverAsync(caller, message.ForId!, message.EventName!,
            message.Additional,
            cancellationToken);
        if (delivered.IsFailure)
        {
            return delivered.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Delivered usage for {For}", message.ForId!);

        return true;
    }

    private async Task<Result<bool, Error>> DeliverAuditInternalAsync(ICallerContext caller, AuditMessage message,
        CancellationToken cancellationToken)
    {
        if (message.AuditCode.IsInvalidParameter(x => x.HasValue(), nameof(AuditMessage.AuditCode), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_Audit_MissingCode);
        }

        var templateArguments = TemplateArguments.Create(message.Arguments ?? new List<string>());
        if (templateArguments.IsFailure)
        {
            return templateArguments.Error;
        }

        var created = AuditRoot.Create(_recorder, _idFactory, message.AgainstId.ToId(), message.TenantId.ToId(),
            message.AuditCode!, message.MessageTemplate.ToOptional(), templateArguments.Value);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var audit = created.Value;
        var saved = await _auditRepository.SaveAsync(audit, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        audit = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Audit {Id} was created", audit.Id);

        return true;
    }

    private async Task<Result<bool, Error>> NotifyProvisioningInternalAsync(ICallerContext caller,
        ProvisioningMessage message, CancellationToken cancellationToken)
    {
        if (message.TenantId.IsInvalidParameter(x => x.HasValue(), nameof(ProvisioningMessage.TenantId), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_Provisioning_MissingTenantId);
        }

        var tenantSettings = new TenantSettings(message.Settings.ToDictionary(pair => pair.Key,
            pair =>
            {
                var value = pair.Value.Value;
                if (value is JsonElement jsonElement)
                {
                    value = jsonElement.ValueKind switch
                    {
                        JsonValueKind.String => jsonElement.GetString(),
                        JsonValueKind.Number => jsonElement.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => null
                    };
                }

                return new TenantSetting(value);
            }));
        var notified =
            await _provisioningNotificationService.NotifyAsync(caller, message.TenantId!, tenantSettings,
                cancellationToken);
        if (notified.IsFailure)
        {
            return notified.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Notified provisioning for {Tenant}", message.TenantId!);

        return true;
    }

#if TESTINGONLY
    private static async Task DrainAllOnQueueAsync<TQueuedMessage>(IMessageQueueStore<TQueuedMessage> repository,
        Func<TQueuedMessage, Task<Result<bool, Error>>> handler, CancellationToken cancellationToken)
        where TQueuedMessage : IQueuedMessage, new()
    {
        var found = new Result<bool, Error>(true);
        while (found.Value)
        {
            found = await repository.PopSingleAsync(OnMessageReceivedAsync, cancellationToken);
            continue;

            async Task<Result<Error>> OnMessageReceivedAsync(TQueuedMessage message, CancellationToken _)
            {
                var handled = await handler(message);
                if (handled.IsFailure)
                {
                    handled.Error.Throw();
                }

                return Result.Ok;
            }
        }
    }
#endif

    private static Result<TQueuedMessage, Error> RehydrateMessage<TQueuedMessage>(string messageAsJson)
        where TQueuedMessage : IQueuedMessage
    {
        try
        {
            var message = messageAsJson.FromJson<TQueuedMessage>();
            if (message.NotExists())
            {
                return Error.RuleViolation(
                    Resources.AncillaryApplication_InvalidQueuedMessage.Format(typeof(TQueuedMessage).Name,
                        messageAsJson));
            }

            return message;
        }
        catch (Exception)
        {
            return Error.RuleViolation(
                Resources.AncillaryApplication_InvalidQueuedMessage.Format(typeof(TQueuedMessage).Name, messageAsJson));
        }
    }
}

public static class AncillaryConversionExtensions
{
    public static Audit ToAudit(this Persistence.ReadModels.Audit audit)
    {
        return new Audit
        {
            Id = audit.Id,
            AuditCode = audit.AuditCode,
            AgainstId = audit.AgainstId,
            OrganizationId = audit.OrganizationId,
            MessageTemplate = audit.MessageTemplate,
            TemplateArguments = audit.TemplateArguments
        };
    }

    public static DeliveredEmail ToDeliveredEmail(this EmailDelivery email)
    {
        return new DeliveredEmail
        {
            Attempts = email.Attempts.HasValue
                ? email.Attempts.Value.Attempts.ToList()
                : new List<DateTime>(),
            Body = email.Body,
            IsDelivered = email.Delivered.HasValue,
            Subject = email.Subject,
            ToDisplayName = email.ToDisplayName,
            ToEmailAddress = email.ToEmailAddress,
            Id = email.Id
        };
    }
}