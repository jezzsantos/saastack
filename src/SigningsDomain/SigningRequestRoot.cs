using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Signings;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;

namespace SigningsDomain;

public sealed class SigningRequestRoot : AggregateRootBase
{
    public static Result<SigningRequestRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        Identifier organizationId)
    {
        var root = new SigningRequestRoot(recorder, idFactory);
        root.RaiseCreateEvent(SigningsDomain.Events.Created(root.Id, organizationId));
        return root;
    }

    private SigningRequestRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private SigningRequestRoot(IRecorder recorder, IIdentifierFactory idFactory,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public Identifier OrganizationId { get; private set; } = Identifier.Empty();

    public static AggregateRootFactory<SigningRequestRoot> Rehydrate()
    {
        return (identifier, container, _) => new SigningRequestRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        //TODO: add your other invariant rules here

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                OrganizationId = created.OrganizationId.ToId();
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }
}