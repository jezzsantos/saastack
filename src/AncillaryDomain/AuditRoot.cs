using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;

namespace AncillaryDomain;

public sealed class AuditRoot : AggregateRootBase
{
    public static Result<AuditRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        Identifier againstId, Identifier organizationId, string auditCode, Optional<string> messageTemplate,
        TemplateArguments templateArguments)
    {
        var root = new AuditRoot(recorder, idFactory);
        root.RaiseCreateEvent(AncillaryDomain.Events.Audits.Created.Create(root.Id, againstId, organizationId, auditCode,
            messageTemplate, templateArguments));
        return root;
    }

    private AuditRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AuditRoot(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public Optional<Identifier> AgainstId { get; private set; }

    public Optional<string> AuditCode { get; private set; }

    public Optional<string> MessageTemplate { get; private set; }

    public Optional<TemplateArguments> TemplateArguments { get; private set; }

    public Optional<Identifier> OrganizationId { get; private set; }

    public static AggregateRootFactory<AuditRoot> Rehydrate()
    {
        return (identifier, container, _) => new AuditRoot(container.Resolve<IRecorder>(),
            container.Resolve<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Events.Audits.Created created:
            {
                OrganizationId = created.OrganizationId.ToId();
                AgainstId = created.AgainstId.ToId();
                AuditCode = created.AuditCode;
                MessageTemplate = Optional<string>.Some(created.MessageTemplate);
                TemplateArguments = AncillaryDomain.TemplateArguments.Create(created.TemplateArguments).Value;
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }
}