using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace AncillaryInfrastructure.Persistence.ReadModels;

public class AuditProjection : IReadModelProjection
{
    private readonly IReadModelProjectionStore<Audit> _audits;

    public AuditProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _audits = new ReadModelProjectionStore<Audit>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof(AuditRoot);

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Events.Created e:
                return await _audits.HandleCreateAsync(e.RootId.ToId(), dto =>
                {
                    dto.OrganizationId = e.OrganizationId;
                    dto.AuditCode = e.AuditCode;
                    dto.AgainstId = e.AgainstId;
                    dto.MessageTemplate = e.MessageTemplate;
                    dto.TemplateArguments = TemplateArguments.Create(e.TemplateArguments).Value;
                }, cancellationToken);

            default:
                return false;
        }
    }
}