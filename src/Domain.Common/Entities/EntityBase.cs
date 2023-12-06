using System.Diagnostics.CodeAnalysis;
using Common;
using Common.Extensions;
using Domain.Common.Events;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;

namespace Domain.Common.Entities;

public delegate Result<Error> RootEventHandler(IDomainEvent @event);

/// <summary>
///     Defines an DDD entity.
///     Entities are the same when their identities are equal.
///     Entities are created by the root aggregate, and have events passed down to them from the aggregate that change it
///     state.
///     Entities support being persisted, and are loaded and saved from a list of events.
///     Entities create changes to their state, by raising and handling domain events, and relaying them back up to the
///     root aggregate to be collected.
///     This entity always produces domain events to represent changes made to it.
///     This entity may be reconstituted into memory from either a stream of events (via
///     <see cref="AggregateRootBase.LoadChanges" />),
///     or it can be reconstituted into memory using a <see cref="EntityFactory{TEntity}.Rehydrate" />
///     method.
///     This entity's state can persisted from memory into a stream of events (using
///     <see cref="AggregateRootBase.GetChanges" />),
///     or it's state can be persisted from memory using the <see cref="Dehydrate" /> method.
/// </summary>
public abstract class EntityBase : IEntity, IEventingEntity, IDehydratableEntity
{
    private RootEventHandler? _rootEventHandler;

    /// <summary>
    ///     Creates a new instance of the entity and generates its own <see cref="Identifier" />,
    ///     Should only be used by entity class factories.
    /// </summary>
    protected EntityBase(IRecorder recorder, IIdentifierFactory idFactory,
        RootEventHandler? eventHandler) : this(recorder, idFactory, Identifier.Empty())
    {
        SetRootEventHandler(eventHandler);
    }

    /// <summary>
    ///     Creates a new instance of the entity with the specified <see cref="Identifier" />, and persisted
    ///     <see cref="rehydratingProperties" />values.
    ///     Should be only used by a ctor used in Rehydration, when persisting in-memory state.
    /// </summary>
    protected EntityBase(ISingleValueObject<string> identifier, IDependencyContainer container,
        HydrationProperties rehydratingProperties)
        : this(container.Resolve<IRecorder>(), container.Resolve<IIdentifierFactory>(), identifier)
    {
        Id = rehydratingProperties.GetValueOrDefault(nameof(Id), Identifier.Empty());
        LastPersistedAtUtc = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(LastPersistedAtUtc));
        IsDeleted = rehydratingProperties.GetValueOrDefault<bool>(nameof(IsDeleted));
        CreatedAtUtc = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(CreatedAtUtc));
        LastModifiedAtUtc = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(LastModifiedAtUtc));
    }

    /// <summary>
    ///     Creates a new instance of the entity and may generate its own <see cref="Identifier" />.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     when there is an error generating a new identifier from the
    ///     <see cref="idFactory" />
    /// </exception>
    private EntityBase(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier)
    {
        Recorder = recorder;
        IdFactory = idFactory;

        var isInstantiating = identifier.Value == string.Empty;
        if (isInstantiating)
        {
            var create = idFactory.Create(this);
            if (!create.IsSuccessful)
            {
                throw new InvalidOperationException(create.Error.Message);
            }

            Id = create.Value;
        }
        else
        {
            Id = Identifier.Create(identifier.Value);
        }

        var now = DateTime.UtcNow;
        LastPersistedAtUtc = Optional<DateTime>.None;
        IsDeleted = Optional<bool>.None;
        CreatedAtUtc = isInstantiating
            ? now
            : DateTime.MinValue;
        LastModifiedAtUtc = isInstantiating
            ? now
            : DateTime.MinValue;
    }

    public Identifier Id { get; }

    protected IIdentifierFactory IdFactory { get; }

    protected IRecorder Recorder { get; }

    /// <summary>
    ///     Verifies that all invariants are still valid
    /// </summary>
    public virtual Result<Error> EnsureInvariants()
    {
        return Result.Ok;
    }

    /// <summary>
    ///     Handles domain events and updates in-memory state of the entity.
    /// </summary>
    protected abstract Result<Error> OnStateChanged(IDomainEvent @event);

    /// <summary>
    ///     Dehydrates the entity to a set of persistable properties
    /// </summary>
    public virtual HydrationProperties Dehydrate()
    {
        return new HydrationProperties
        {
            { nameof(Id), Id },
            { nameof(LastPersistedAtUtc), LastPersistedAtUtc },
            { nameof(IsDeleted), IsDeleted },
            { nameof(CreatedAtUtc), CreatedAtUtc },
            { nameof(LastModifiedAtUtc), LastModifiedAtUtc }
        };
    }

    public Optional<bool> IsDeleted { get; private protected set; }

    public Optional<DateTime> LastPersistedAtUtc { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime LastModifiedAtUtc { get; private set; }

    ISingleValueObject<string> IIdentifiableEntity.Id => Id;

    Result<Error> IDomainEventConsumingEntity.HandleStateChanged(IDomainEvent @event)
    {
        return OnStateChanged(@event);
    }

    /// <summary>
    ///     Raises an @event, and then validates the invariants
    /// </summary>
    Result<Error> IDomainEventProducingEntity.RaiseEvent(IDomainEvent @event, bool validate)
    {
        var onStateChanged = OnStateChanged(@event);
        if (!onStateChanged.IsSuccessful)
        {
            return onStateChanged;
        }

        if (validate)
        {
            var ensureInvariants = EnsureInvariants();
            if (!ensureInvariants.IsSuccessful)
            {
                return ensureInvariants.Error;
            }
        }

        if (_rootEventHandler.Exists())
        {
            var handler = _rootEventHandler.Invoke(@event);
            if (!handler.IsSuccessful)
            {
                return handler.Error;
            }
        }

        LastModifiedAtUtc = DateTime.UtcNow;
        return Result.Ok;
    }

    public sealed override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is EntityBase other && Equals(other));
    }

    public override int GetHashCode()
    {
        return Id.HasValue()
            ? Id.GetHashCode()
            : 0;
    }

    /// <summary>
    ///     Raises a new change <see cref="@event" />
    /// </summary>
    public Result<Error> RaiseChangeEvent(IDomainEvent @event)
    {
        return ((IDomainEventProducingEntity)this).RaiseEvent(@event, true);
    }

    /// <summary>
    ///     Sets the handler to raise events to the parent/ancestor aggregate root
    /// </summary>
    public void SetRootEventHandler(RootEventHandler? eventHandler)
    {
        _rootEventHandler = eventHandler;
    }

    /// <summary>
    ///     Used to handle any unknown events in <see cref="OnStateChanged" /> handler
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Preserve instance method")]
    // ReSharper disable once MemberCanBeMadeStatic.Global
    protected Result<Error> HandleUnKnownStateChangedEvent(IDomainEvent @event)
    {
        if (@event is Global.StreamDeleted)
        {
            return Result.Ok;
        }

        return Error.RuleViolation(
            Resources.EventingEntityBase_HandleUnKnownStateChangedEvent_UnknownEvent.Format(@event.GetType()));
    }

    private bool Equals(EntityBase entity)
    {
        if (!entity.Id.HasValue())
        {
            return false;
        }

        if (!Id.HasValue())
        {
            return false;
        }

        return entity.Id == Id;
    }

#if TESTINGONLY
    public void Delete()
    {
        IsDeleted = true;
    }

    public void Undelete()
    {
        IsDeleted = false;
    }
#endif
}