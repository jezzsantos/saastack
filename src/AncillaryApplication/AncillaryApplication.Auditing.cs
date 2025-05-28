using AncillaryDomain;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Shared.Extensions;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Audit = Application.Resources.Shared.Audit;

namespace AncillaryApplication;

partial class AncillaryApplication
{
    public async Task<Result<bool, Error>> DeliverAuditAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken)
    {
        var rehydrated = messageAsJson.RehydrateQueuedMessage<AuditMessage>();
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
    public async Task<Result<Error>> DrainAllAuditsAsync(ICallerContext caller, CancellationToken cancellationToken)
    {
        await _auditMessageQueueRepository.DrainAllQueuedMessagesAsync(
            message => DeliverAuditInternalAsync(caller, message, cancellationToken), cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Drained all audit messages");

        return Result.Ok;
    }
#endif

    public async Task<Result<SearchResults<Audit>, Error>> SearchAllAuditsAsync(ICallerContext caller,
        DateTime? sinceUtc, string? organizationId, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        var sinceWhen = sinceUtc ?? DateTime.UtcNow.SubtractDays(14);
        var searched =
            await _auditRepository.SearchAllAsync(sinceWhen, organizationId, searchOptions, cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        var audits = searched.Value;
        _recorder.TraceInformation(caller.ToCall(), "All audits were fetched");

        return audits.ToSearchResults(searchOptions, audit => audit.ToAudit());
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

        var created = AuditRoot.Create(_recorder, _idFactory, message.AgainstId.ToId(), message.TenantId.HasValue()
                ? message.TenantId.ToId()
                : Optional<Identifier>.None,
            message.AuditCode!, message.MessageTemplate.ToOptional(), templateArguments.Value, caller.HostRegion);
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
        _recorder.TraceInformation(caller.ToCall(), "Audit {Id} (from {Region}) was created", audit.Id,
            message.OriginHostRegion ?? DatacenterLocations.Unknown.Code);

        return true;
    }
}

public static class AncillaryAuditingConversionExtensions
{
    public static Audit ToAudit(this Persistence.ReadModels.Audit audit)
    {
        return new Audit
        {
            Created = audit.Created.HasValue
                ? audit.Created.Value!.Value
                : DateTime.UtcNow,
            Id = audit.Id,
            AuditCode = audit.AuditCode,
            AgainstId = audit.AgainstId,
            OrganizationId = audit.OrganizationId,
            MessageTemplate = audit.MessageTemplate,
            TemplateArguments = audit.TemplateArguments
        };
    }
}