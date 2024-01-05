using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;

namespace EndUsersDomain;

public sealed class EndUserRoot : AggregateRootBase
{
    public static Result<EndUserRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        UserClassification classification)
    {
        var root = new EndUserRoot(recorder, idFactory);
        root.RaiseCreateEvent(EndUsersDomain.Events.Created.Create(root.Id, classification));
        return root;
    }

    private EndUserRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private EndUserRoot(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public UserAccess Access { get; private set; }

    public UserClassification Classification { get; private set; }

    public FeatureLevels Features { get; private set; } = FeatureLevels.Create().Value;

    public Roles Roles { get; private set; } = Roles.Create().Value;

    public UserStatus Status { get; private set; }

    public static AggregateRootFactory<EndUserRoot> Rehydrate()
    {
        return (identifier, container, _) => new EndUserRoot(container.Resolve<IRecorder>(),
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
            case Events.Created created:
            {
                Access = created.Access.ToEnumOrDefault(UserAccess.Enabled);
                Status = created.Status.ToEnumOrDefault(UserStatus.Unregistered);
                Classification = created.Classification.ToEnumOrDefault(UserClassification.Person);
                Features = FeatureLevels.Create().Value;
                Roles = Roles.Create().Value;
                return Result.Ok;
            }

            case Events.Registered changed:
            {
                Access = changed.Access.ToEnumOrDefault(UserAccess.Enabled);
                Status = changed.Status.ToEnumOrDefault(UserStatus.Unregistered);
                Classification = changed.Classification.ToEnumOrDefault(UserClassification.Person);

                var roles = Roles.Create(changed.Roles);
                if (!roles.IsSuccessful)
                {
                    return roles.Error;
                }

                Roles = roles.Value;
                var levels = FeatureLevels.Create(changed.FeatureLevels);
                if (!levels.IsSuccessful)
                {
                    return levels.Error;
                }

                Features = levels.Value;
                Recorder.TraceDebug(null, "EndUser {Id} was registered", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> Register(UserClassification classification, Roles roles, FeatureLevels levels,
        Optional<EmailAddress> username)
    {
        if (Status != UserStatus.Unregistered)
        {
            return Error.RuleViolation(Resources.EndUserRoot_AlreadyRegistered);
        }

        return RaiseChangeEvent(EndUsersDomain.Events.Registered.Create(Id, username, classification,
            UserAccess.Enabled, UserStatus.Registered, roles, levels));
    }
}