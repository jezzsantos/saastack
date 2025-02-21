using Common;
using Domain.Common.Entities;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Bookings;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;
using QueryAny;

namespace BookingsDomain;

[EntityName("Trip")]
public sealed class Trip : EntityBase
{
    public static Result<Trip, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        RootEventHandler rootEventHandler)
    {
        return new Trip(recorder, idFactory, rootEventHandler);
    }

    private Trip(IRecorder recorder, IIdentifierFactory idFactory,
        RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    private Trip(ISingleValueObject<string> identifier, IDependencyContainer container,
        HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
        RootId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(RootId));
        OrganizationId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(OrganizationId));
        BeganAt = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(BeganAt));
        EndedAt = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(EndedAt));
        From = rehydratingProperties.GetValueOrDefault<Location>(nameof(From));
        To = rehydratingProperties.GetValueOrDefault<Location>(nameof(To));
    }

    public Optional<DateTime> BeganAt { get; private set; }

    public Optional<DateTime> EndedAt { get; private set; }

    public Optional<Location> From { get; private set; }

    public Optional<Identifier> OrganizationId { get; private set; }

    public Optional<Identifier> RootId { get; private set; }

    public Optional<Location> To { get; private set; }

    [UsedImplicitly]
    public static EntityFactory<Trip> Rehydrate()
    {
        return (identifier, container, properties) => new Trip(identifier, container, properties);
    }

    public override HydrationProperties Dehydrate()
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
            case TripAdded added:
            {
                RootId = added.RootId.ToId();
                OrganizationId = added.OrganizationId.ToId();
                return Result.Ok;
            }

            case TripBegan changed:
            {
                var from = Location.Create(changed.BeganFrom);
                if (from.IsFailure)
                {
                    return from.Error;
                }

                BeganAt = changed.BeganAt;
                From = from.Value;
                return Result.Ok;
            }

            case TripEnded changed:
            {
                var to = Location.Create(changed.EndedTo);
                if (to.IsFailure)
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
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        if (BeganAt.HasValue && !From.HasValue)
        {
            return Error.RuleViolation(Resources.TripEntity_NoStartingLocation);
        }

        if (EndedAt.HasValue && !BeganAt.HasValue)
        {
            return Error.RuleViolation(Resources.TripEntity_NotBegun);
        }

        if (EndedAt.HasValue && !To.HasValue)
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
        return RaiseChangeEvent(Events.TripBegan(RootId.Value, OrganizationId.Value, Id, starts, from));
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
        return RaiseChangeEvent(Events.TripEnded(RootId.Value, OrganizationId.Value, Id, BeganAt.Value,
            From.Value, ends, to));
    }

#if TESTINGONLY
    internal void TestingOnly_Assign(Optional<Location> from, Optional<Location> to)
    {
        From = from;
        To = to;
    }
#endif
}