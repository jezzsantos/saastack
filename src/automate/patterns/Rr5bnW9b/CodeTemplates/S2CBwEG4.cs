using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Events.Shared.{{SubdomainName | string.pascalplural}};
using JetBrains.Annotations;

namespace {{SubdomainName | string.pascalplural}}Domain;

public sealed class {{SubdomainName | string.pascalsingular}}Root : AggregateRootBase
{
    public static Result<{{SubdomainName | string.pascalsingular}}Root, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        Identifier organizationId)
    {
        var root = new {{SubdomainName | string.pascalsingular}}Root(recorder, idFactory);
        root.RaiseCreateEvent({{SubdomainName | string.pascalplural}}Domain.Events.Created(root.Id, organizationId));
        return root;
    }

    private {{SubdomainName | string.pascalsingular}}Root(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private {{SubdomainName | string.pascalsingular}}Root(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public Identifier OrganizationId { get; private set; } = Identifier.Empty();

    //TODO: Add other properties to this root

    [UsedImplicitly]
    public static AggregateRootFactory<{{SubdomainName | string.pascalsingular}}Root> Rehydrate()
    {
        return (identifier, container, _) => new {{SubdomainName | string.pascalsingular}}Root(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                OrganizationId = created.OrganizationId.ToId();
                //TODO: assign other properties on the root
                return Result.Ok;
            }

            //TODO: handle other events, and trace out changes
            // case SomethingChanged changed:
            // {
            //     Recorder.TraceDebug(null, "{{SubdomainName | string.pascalsingular}} {Id} changed something to {Something}", Id, changed.Something);
            //     return Result.Ok;
            // }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }
}