using AncillaryApplication.Persistence;
using AncillaryDomain;
using Application.Common;
using Application.Interfaces;
using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Shared;
using Task = System.Threading.Tasks.Task;

namespace AncillaryApplication;

public class AncillaryApplication : IAncillaryApplication
{
    private readonly IAuditMessageQueueRepository _auditMessageQueueRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IEmailDeliveryService _emailDeliveryService;
    private readonly IEmailMessageQueue _emailMessageQueue;
    private readonly IIdentifierFactory _idFactory;
    private readonly IRecorder _recorder;
    private readonly IUsageDeliveryService _usageDeliveryService;
    private readonly IUsageMessageQueue _usageMessageQueue;
    private readonly IEmailDeliveryRepository _emailDeliveryRepository;

    public AncillaryApplication(IRecorder recorder, IIdentifierFactory idFactory,
        IUsageMessageQueue usageMessageQueue, IUsageDeliveryService usageDeliveryService,
        IAuditMessageQueueRepository auditMessageQueueRepository, IAuditRepository auditRepository,
        IEmailMessageQueue emailMessageQueue, IEmailDeliveryService emailDeliveryService,
        IEmailDeliveryRepository emailDeliveryRepository)
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
    }

    public async Task<Result<bool, Error>> DeliverEmailAsync(ICallerContext context, string messageAsJson,
        CancellationToken cancellationToken)
    {
        var rehydrated = RehydrateMessage<EmailMessage>(messageAsJson);
        if (!rehydrated.IsSuccessful)
        {
            return rehydrated.Error;
        }

        var delivered = await DeliverEmailAsync(context, rehydrated.Value, cancellationToken);
        if (!delivered.IsSuccessful)
        {
            return delivered.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "Delivered email message: {Message}", messageAsJson);
        return true;
    }

    public async Task<Result<bool, Error>> DeliverUsageAsync(ICallerContext context, string messageAsJson,
        CancellationToken cancellationToken)
    {
        var rehydrated = RehydrateMessage<UsageMessage>(messageAsJson);
        if (!rehydrated.IsSuccessful)
        {
            return rehydrated.Error;
        }

        var delivered = await DeliverUsageAsync(context, rehydrated.Value, cancellationToken);
        if (!delivered.IsSuccessful)
        {
            return delivered.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "Delivered usage message: {Message}", messageAsJson);
        return true;
    }

#if TESTINGONLY
    public async Task<Result<SearchResults<Audit>, Error>> SearchAllAuditsAsync(ICallerContext context,
        string organizationId, SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken)
    {
        var searched = await _auditRepository.SearchAllAsync(organizationId.ToId(), searchOptions, cancellationToken);
        if (!searched.IsSuccessful)
        {
            return searched.Error;
        }

        var audits = searched.Value;

        return searchOptions.ApplyWithMetadata(audits.Select(audit => audit.ToAudit()));
    }
#endif

    public async Task<Result<bool, Error>> DeliverAuditAsync(ICallerContext context, string messageAsJson,
        CancellationToken cancellationToken)
    {
        var rehydrated = RehydrateMessage<AuditMessage>(messageAsJson);
        if (!rehydrated.IsSuccessful)
        {
            return rehydrated.Error;
        }

        var delivered = await DeliverAuditAsync(context, rehydrated.Value, cancellationToken);
        if (!delivered.IsSuccessful)
        {
            return delivered.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "Delivered audit message: {Message}", messageAsJson);
        return true;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DrainAllEmailsAsync(ICallerContext context, CancellationToken cancellationToken)
    {
        await DrainAllAsync(_emailMessageQueue,
            message => DeliverEmailAsync(context, message, cancellationToken), cancellationToken);

        _recorder.TraceInformation(context.ToCall(), "Drained all email messages");

        return Result.Ok;
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DrainAllUsagesAsync(ICallerContext context, CancellationToken cancellationToken)
    {
        await DrainAllAsync(_usageMessageQueue,
            message => DeliverUsageAsync(context, message, cancellationToken), cancellationToken);

        _recorder.TraceInformation(context.ToCall(), "Drained all usage messages");

        return Result.Ok;
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DrainAllAuditsAsync(ICallerContext context, CancellationToken cancellationToken)
    {
        await DrainAllAsync(_auditMessageQueueRepository,
            message => DeliverAuditAsync(context, message, cancellationToken), cancellationToken);

        _recorder.TraceInformation(context.ToCall(), "Drained all audit messages");

        return Result.Ok;
    }
#endif

    private async Task<Result<bool, Error>> DeliverEmailAsync(ICallerContext context, EmailMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Html.IsInvalidParameter(x => x.Exists(), nameof(EmailMessage.Html), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_MissingEmailHtml);
        }

        var retrieved = await _emailDeliveryRepository.FindDeliveryByMessageIdAsync(message.MessageId!);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        EmailDeliveryRoot delivery;
        var found = retrieved.Value.HasValue;
        if (found)
        {
            delivery = retrieved.Value.Value;
        }
        else
        {
            var created = EmailDeliveryRoot.Create(_recorder, _idFactory, message.MessageId!);
            if (!created.IsSuccessful)
            {
                return created.Error;
            }

            delivery = created.Value;
            var emailAddress = EmailAddress.Create(message.Html.ToEmailAddress!);
            if (!emailAddress.IsSuccessful)
            {
                return emailAddress.Error;
            }

            var recipientsSetup = delivery.SetupRecipients(emailAddress.Value);
            if (!recipientsSetup.IsSuccessful)
            {
                return recipientsSetup.Error;
            }
        }

        if (delivery.IsDelivered)
        {
            return true;
        }

        var attemptedDelivery = delivery.AttemptDelivery();
        if (!attemptedDelivery.IsSuccessful)
        {
            return attemptedDelivery.Error;
        }

        var delivered = await _emailDeliveryService.DeliverAsync(context, message.Html.Subject!, message.Html.HtmlBody!,
            message.Html.ToEmailAddress!, message.Html.ToDisplayName, message.Html.FromEmailAddress!,
            message.Html.FromDisplayName,
            cancellationToken);
        if (!delivered.IsSuccessful)
        {
            return delivered.Error;
        }

        
        var completeDelivery = delivery.CompleteDelivery(delivered.Value.TransactionId.ToOptional());
        if (!completeDelivery.IsSuccessful)
        {
            return completeDelivery.Error;
        }
        
        var saved = await _emailDeliveryRepository.SaveAsync(delivery, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return retrieved.Error;
        }
        
        _recorder.TraceInformation(context.ToCall(), "Delivered email for {For}", message.Html.ToEmailAddress!);

        return true;
    }

    private async Task<Result<bool, Error>> DeliverUsageAsync(ICallerContext context, UsageMessage message,
        CancellationToken cancellationToken)
    {
        if (message.ForId.IsInvalidParameter(x => x.HasValue(), nameof(UsageMessage.ForId), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_MissingUsageForId);
        }

        if (message.EventName.IsInvalidParameter(x => x.HasValue(), nameof(UsageMessage.EventName), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_MissingUsageEventName);
        }

        await _usageDeliveryService.DeliverAsync(context, message.ForId!, message.EventName!, message.Additional,
            cancellationToken);

        _recorder.TraceInformation(context.ToCall(), "Delivered usage for {For}", message.ForId!);

        return true;
    }

    private async Task<Result<bool, Error>> DeliverAuditAsync(ICallerContext context, AuditMessage message,
        CancellationToken cancellationToken)
    {
        if (message.AuditCode.IsInvalidParameter(x => x.HasValue(), nameof(AuditMessage.AuditCode), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_MissingAuditCode);
        }

        if (message.TenantId.IsInvalidParameter(x => x.HasValue(), nameof(AuditMessage.TenantId), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_MissingTenantId);
        }

        var templateArguments = TemplateArguments.Create(message.Arguments ?? new List<string>());
        if (!templateArguments.IsSuccessful)
        {
            return templateArguments.Error;
        }

        var created = AuditRoot.Create(_recorder, _idFactory, message.AgainstId.ToId(), message.TenantId.ToId(),
            message.AuditCode!, message.MessageTemplate.ToOptional(), templateArguments.Value);
        if (!created.IsSuccessful)
        {
            return created.Error;
        }

        var audit = created.Value;
        var updated = await _auditRepository.SaveAsync(audit, cancellationToken);
        if (!updated.IsSuccessful)
        {
            return updated.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "Audit {Id} was created", updated.Value.Id);

        return true;
    }

    private static async Task DrainAllAsync<TQueuedMessage>(IMessageQueueStore<TQueuedMessage> repository,
        Func<TQueuedMessage, Task<Result<bool, Error>>> handler,
        CancellationToken cancellationToken)
        where TQueuedMessage : IQueuedMessage, new()
    {
        var found = new Result<bool, Error>(true);
        while (found.Value)
        {
            async Task<Result<Error>> OnMessageReceivedAsync(TQueuedMessage message, CancellationToken _)
            {
                var handled = await handler(message);
                return handled.Match(_ => Result.Ok, error => error);
            }

            found = await repository.PopSingleAsync(OnMessageReceivedAsync, cancellationToken);
        }
    }

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
}