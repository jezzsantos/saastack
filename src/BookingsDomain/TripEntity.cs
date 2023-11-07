using BookingsDomain.Events;
using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using QueryAny;

namespace BookingsDomain;

[EntityName("Trip")]
public sealed class TripEntity : EntityBase
{
    public static Result<TripEntity, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        RootEventHandler rootEventHandler)
    {
        return new TripEntity(recorder, idFactory, rootEventHandler);
    }

    private TripEntity(IRecorder recorder, IIdentifierFactory idFactory,
        RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    private TripEntity(Identifier identifier, IDependencyContainer container,
        IReadOnlyDictionary<string, object?> rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
        RootId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(RootId))!;
        OrganizationId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(OrganizationId))!;
        BeganAt = rehydratingProperties.GetValueOrDefault<DateTime?>(nameof(BeganAt));
        EndedAt = rehydratingProperties.GetValueOrDefault<DateTime?>(nameof(EndedAt));
        From = rehydratingProperties.GetValueOrDefault<Location>(nameof(From));
        To = rehydratingProperties.GetValueOrDefault<Location>(nameof(To));
    }

    public DateTime? BeganAt { get; private set; }

    public DateTime? EndedAt { get; private set; }

    public Location? From { get; private set; }

    public Identifier? OrganizationId { get; private set; }

    public Identifier? RootId { get; private set; }

    public Location? To { get; private set; }

    public static EntityFactory<TripEntity> Rehydrate()
    {
        return (identifier, container, properties) => new TripEntity(identifier, container, properties);
    }

    public override Dictionary<string, object?> Dehydrate()
    {
        var properties = base.Dehydrate();
        properties.Add(nameof(RootId), RootId);
        properties.Add(nameof(OrganizationId), OrganizationId);
        properties.Add(nameof(BeganAt), BeganAt);
        properties.Add(nameof(EndedAt), EndedAt);
        properties.Add(nameof(From), From);
        properties.Add(nameof(To), To);
        return properties;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        switch (@event)
        {
            case Booking.TripAdded added:
            {
                RootId = added.RootId.ToId();
                OrganizationId = added.OrganizationId.ToId();
                return Result.Ok;
            }

            case Booking.TripBegan changed:
            {
                var from = Location.Create(changed.BeganFrom);
                if (!from.IsSuccessful)
                {
                    return from.Error;
                }

                BeganAt = changed.BeganAt;
                From = from.Value;
                return Result.Ok;
            }

            case Booking.TripEnded changed:
            {
                var to = Location.Create(changed.EndedTo);
                if (!to.IsSuccessful)
                {
                    return to.Error;
                }

                EndedAt = changed.EndedAt;
                To = to.Value;
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        if (BeganAt.HasValue && From.NotExists())
        {
            return Error.RuleViolation(Resources.TripEntity_NoStartingLocation);
        }

        if (EndedAt.HasValue && !BeganAt.HasValue)
        {
            return Error.RuleViolation(Resources.TripEntity_NotBegun);
        }

        if (EndedAt.HasValue && To.NotExists())
        {
            return Error.RuleViolation(Resources.TripEntity_NoEndingLocation);
        }

        return Result.Ok;
    }

    public Result<Error> Begin(Location from)
    {
        if (BeganAt.HasValue)
        {
            return Error.RuleViolation(Resources.TripEntity_AlreadyBegan);
        }

        var starts = DateTime.UtcNow;
        return RaiseChangeEvent(Booking.TripBegan.Create(RootId!, OrganizationId!, Id, starts, from));
    }

    public Result<Error> End(Location to)
    {
        if (!BeganAt.HasValue)
        {
            return Error.RuleViolation(Resources.TripEntity_NotBegun);
        }

        if (EndedAt.HasValue)
        {
            return Error.RuleViolation(Resources.TripEntity_AlreadyEnded);
        }

        var ends = DateTime.UtcNow;
        return RaiseChangeEvent(Booking.TripEnded.Create(RootId!, OrganizationId!, Id, BeganAt.GetValueOrDefault(),
            From!, ends, to));
    }

#if TESTINGONLY
    internal void TestingOnly_Assign(Location? from, Location? to)
    {
        From = from;
        To = to;
    }
#endif
}