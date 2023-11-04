using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Common;
using Common.Extensions;
using Domain.Common.Events;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;

namespace Domain.Common.Entities;

/// <summary>
///     Defines an DDD aggregate root
///     Aggregates are the same when their identities are equal.
///     Aggregates support being persisted, and are loaded and saved from a list of events.
///     Aggregates create changes to their state, and changes in state to all descendent entities and value objects, by
///     raising and handling domain events.
///     This aggregate root always produces domain events to represent changes made to it.
///     This aggregate root may be reconstituted into memory from either a stream of events (using
///     <see cref="LoadChanges" />),
///     or it can be reconstituted into memory using a <see cref="DehydratableEntityFactory{TAggregate}.Rehydrate" />
///     method.
///     This aggregate's state can persisted from memory into a stream of events (using <see cref="GetChanges" />),
///     or it's state can be persisted from memory using the <see cref="Dehydrate" /> method.
/// </summary>
public abstract class AggregateRootBase : IAggregateRoot, IEventSourcedAggregateRoot, IDehydratableAggregateRoot
{
    private readonly List<IDomainEvent> _events;
    private readonly bool _isInstantiating;

    /// <summary>
    ///     Creates a new instance of the aggregate and generates its own <see cref="Identifier" />.
    ///     Should only be used by class factories, that should call <see cref="RaiseCreateEvent" /> after calling this ctor.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     when there is an error generating a new identifier from the
    ///     <see cref="idFactory" />
    /// </exception>
    protected AggregateRootBase(IRecorder recorder, IIdentifierFactory idFactory) : this(recorder,
        idFactory, Identifier.Empty())
    {
        var create = idFactory.Create(this);
        if (!create.IsSuccessful)
        {
            throw new InvalidOperationException(create.Error.Message);
        }

        Id = create.Value;
    }

    /// <summary>
    ///     Creates a new instance of the aggregate with the specified <see cref="Identifier" />, and persisted
    ///     <see cref="rehydratingProperties" />values.
    ///     Should be only used by a ctor used in Rehydration, when persisting in-memory state.
    ///     Should never raise any events.
    /// </summary>
    protected AggregateRootBase(Identifier identifier, IDependencyContainer container,
        IReadOnlyDictionary<string, object?> rehydratingProperties) : this(container.Resolve<IRecorder>(),
        container.Resolve<IIdentifierFactory>(), identifier)
    {
        Id = rehydratingProperties.GetValueOrDefault(nameof(Id), Identifier.Empty())!;
        LastPersistedAtUtc = rehydratingProperties.GetValueOrDefault<DateTime?>(nameof(LastPersistedAtUtc));
        IsDeleted = rehydratingProperties.GetValueOrDefault<bool?>(nameof(IsDeleted));
        CreatedAtUtc = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(CreatedAtUtc));
        LastModifiedAtUtc = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(LastModifiedAtUtc));
    }

    /// <summary>
    ///     Creates a new instance of the aggregate with the specified <see cref="Identifier" />.
    ///     Should be only used by a ctor used in Rehydration, when persisting domain events.
    ///     Should never raise any events.
    /// </summary>
    protected AggregateRootBase(IRecorder recorder, IIdentifierFactory idFactory, Identifier identifier)
    {
        Recorder = recorder;
        IdFactory = idFactory;
        Id = identifier;
        _events = new List<IDomainEvent>();
        _isInstantiating = identifier == Identifier.Empty();

        var now = DateTime.UtcNow;
        LastPersistedAtUtc = null;
        IsDeleted = null;
        CreatedAtUtc = _isInstantiating
            ? now
            : DateTime.MinValue;
        LastModifiedAtUtc = _isInstantiating
            ? now
            : DateTime.MinValue;
        EventStream = EventStream.Create();
    }

    protected internal EventStream EventStream { get; private set; }

    public Identifier Id { get; }

    // ReSharper disable once MemberCanBePrivate.Global
    protected IIdentifierFactory IdFactory { get; }

    protected IRecorder Recorder { get; }

    /// <summary>
    ///     Verifies that all invariants are still valid
    /// </summary>
    // ReSharper disable once MemberCanBeProtected.Global
    public virtual Result<Error> EnsureInvariants()
    {
        return EnsureBaseInvariants();
    }

    /// <summary>
    ///     Handles domain events and updates in-memory state of the aggregate.
    ///     Used when reconstituting the aggregate from an event stream (<see cref="isReconstituting" /> will be
    ///     <see cref="True" />), or when raising an event during a state change (<see cref="isReconstituting" /> will be
    ///     <see cref="False" />).
    /// </summary>
    protected abstract Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting);

    public bool? IsDeleted { get; private set; }

    /// <summary>
    ///     Dehydrates the aggregate to a set of persistable properties
    /// </summary>
    public virtual Dictionary<string, object?> Dehydrate()
    {
        return new Dictionary<string, object?>
        {
            { nameof(Id), Id },
            { nameof(LastPersistedAtUtc), LastPersistedAtUtc },
            { nameof(IsDeleted), IsDeleted },
            { nameof(CreatedAtUtc), CreatedAtUtc },
            { nameof(LastModifiedAtUtc), LastModifiedAtUtc }
        };
    }

    public DateTime? LastPersistedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime LastModifiedAtUtc { get; private set; }

    ISingleValueObject<string> IIdentifiableEntity.Id => Id;

    Result<Error> IDomainEventConsumingEntity.HandleStateChanged(IDomainEvent @event)
    {
        return OnStateChanged(@event, false);
    }

    /// <summary>
    ///     Returns the recent changes to the aggregate
    /// </summary>
    public Result<List<EventSourcedChangeEvent>, Error> GetChanges()
    {
        var versioning = EventStream;

        var changes = new List<EventSourcedChangeEvent>();
        foreach (var @event in _events)
        {
            var next = versioning.Next();
            if (!next.IsSuccessful)
            {
                return next.Error;
            }

            versioning = next.Value;

            var nextVersion = versioning.LastEventVersion;
            var versioned = @event.ToVersioned(IdFactory, GetType().Name, nextVersion);
            if (!versioned.IsSuccessful)
            {
                return versioned.Error;
            }

            changes.Add(versioned.Value);
        }

        return changes;
    }

    /// <summary>
    ///     Clears the recent changes ot the aggregate
    /// </summary>
    public Result<Error> ClearChanges()
    {
        LastPersistedAtUtc = DateTime.UtcNow;
        _events.Clear();
        EventStream = EventStream.Create();
        return Result.Ok;
    }

    /// <summary>
    ///     Reconstitutes the aggregates in-memory state from the past <see cref="history" /> of events,
    ///     using the <see cref="migrator" /> to handle any unknown event types no longer present in the codebase
    /// </summary>
    Result<Error> IEventSourcedAggregateRoot.LoadChanges(IEnumerable<EventSourcedChangeEvent> history,
        IEventSourcedChangeEventMigrator migrator)
    {
        if (EventStream.HasChanges)
        {
            return Error.RuleViolation(Resources.EventingAggregateRootBase_ChangesAlreadyLoaded);
        }

        var changes = history.ToList();
        if (changes.HasNone())
        {
            return Result.Ok;
        }

        foreach (var change in changes)
        {
            var @event = change.ToEvent(migrator);
            if (!@event.IsSuccessful)
            {
                return @event.Error;
            }

            var onStateChanged = OnStateChanged(@event.Value, true);
            if (!onStateChanged.IsSuccessful)
            {
                return onStateChanged.Error;
            }

            var updatedChanged = EventStream.UpdateChange(change.Version);
            if (!updatedChanged.IsSuccessful)
            {
                return updatedChanged.Error;
            }

            EventStream = updatedChanged.Value;
        }

        return Result.Ok;
    }

    /// <summary>
    ///     Raises an @event, and then validates the invariants
    /// </summary>
    Result<Error> IDomainEventProducingEntity.RaiseEvent(IDomainEvent @event, bool validate)
    {
        return RaiseEvent(@event, validate, true);
    }

    public IReadOnlyList<IDomainEvent> Events => _events;

    public override bool Equals(object? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other.GetType() != GetType())
        {
            return false;
        }

        return Equals((AggregateRootBase)other);
    }

    public override int GetHashCode()
    {
        return Id.HasValue()
            ? Id.GetHashCode()
            : 0;
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

        return Error.Unexpected(
            Resources.EventingEntityBase_HandleUnKnownStateChangedEvent_UnknownEvent.Format(@event.GetType()));
    }

    /// <summary>
    ///     Raises a new change <see cref="@event" />
    /// </summary>
    protected Result<Error> RaiseChangeEvent(IDomainEvent @event)
    {
        return ((IDomainEventProducingEntity)this).RaiseEvent(@event, true);
    }

    /// <summary>
    ///     Raises a new create <see cref="@event" />
    /// </summary>
    protected Result<Error> RaiseCreateEvent(IDomainEvent @event)
    {
        if (_isInstantiating)
        {
            return ((IDomainEventProducingEntity)this).RaiseEvent(@event, false);
        }

        return Result.Ok;
    }

    /// <summary>
    ///     Raises the <see cref="@event" /> to an new instance of the <see cref="childEntityFactory" />
    /// </summary>
    protected Result<TEntity, Error> RaiseEventToChildEntity<TEntity, TChangeEvent>(bool isReconstituting,
        TChangeEvent @event,
        Func<IIdentifierFactory, Result<TEntity, Error>> childEntityFactory,
        Expression<Func<TChangeEvent, string>> eventChildId)
        where TEntity : IEventSourcedEntity
        where TChangeEvent : IDomainEvent
    {
        var identifierFactory = isReconstituting
            ? GetChildId().ToIdentifierFactory()
            : IdFactory;
        var createdChild = childEntityFactory(identifierFactory);
        if (!createdChild.IsSuccessful)
        {
            return createdChild.Error;
        }

        SetChildId(createdChild.Value.Id);
        return createdChild.Value.HandleStateChanged(@event)
            .Match<Result<TEntity, Error>>(() => createdChild, error => error);

        string GetChildId()
        {
            var property = (PropertyInfo)((MemberExpression)eventChildId.Body).Member;
            return (string)property.GetValue(@event)!;
        }

        void SetChildId(ISingleValueObject<string> entityId)
        {
            var property = (PropertyInfo)((MemberExpression)eventChildId.Body).Member;
            property.SetValue(@event, entityId.Value);
        }
    }

    /// <summary>
    ///     Raises the <see cref="@event" /> to an new instance of the <see cref="childEntity" />
    /// </summary>
    // ReSharper disable once MemberCanBeMadeStatic.Global
    protected Result<Error> RaiseEventToChildEntity<TEntity, TChangeEvent>(TChangeEvent @event, TEntity childEntity)
        where TEntity : IEventSourcedEntity
        where TChangeEvent : IDomainEvent
    {
        return childEntity.HandleStateChanged(@event);
    }

    /// <summary>
    ///     Raises a tombstone event to permanently delete the whole aggregate.
    /// </summary>
    protected Result<Error> RaisePermanentDeleteEvent(Identifier deletedById)
    {
        var raiseEvent = RaiseEvent(Global.StreamDeleted.Create(Identifier.Create(Id), deletedById), false,
            false);
        if (!raiseEvent.IsSuccessful)
        {
            return raiseEvent;
        }

        IsDeleted = true;
        return Result.Ok;
    }

    private bool Equals(AggregateRootBase other)
    {
        if (!other.Id.HasValue())
        {
            return false;
        }

        if (!Id.HasValue())
        {
            return false;
        }

        return other.Id == Id;
    }

    private Result<Error> EnsureBaseInvariants()
    {
        if (!Id.HasValue())
        {
            return Error.RuleViolation(Resources.EventingAggregateRootBase_HasNoIdentifier);
        }

        return Result.Ok;
    }

    private Result<Error> RaiseEvent(IDomainEvent @event, bool validate, bool changeState)
    {
        if (changeState)
        {
            var onStateChanged = OnStateChanged(@event, false);
            if (!onStateChanged.IsSuccessful)
            {
                return onStateChanged;
            }
        }

        if (validate)
        {
            var ensureInvariants = EnsureInvariants();
            if (!ensureInvariants.IsSuccessful)
            {
                return ensureInvariants.Error;
            }
        }
        else
        {
            var ensureBaseInvariants = EnsureBaseInvariants();
            if (!ensureBaseInvariants.IsSuccessful)
            {
                return ensureBaseInvariants.Error;
            }
        }

        LastModifiedAtUtc = DateTime.UtcNow;
        _events.Add(@event);

        return Result.Ok;
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