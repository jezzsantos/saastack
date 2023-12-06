using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using QueryAny;

namespace Infrastructure.Persistence.Common.UnitTests;

[EntityName("acontainername")]
public class TestEventingAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
{
    public TestEventingAggregateRoot(Identifier identifier)
    {
        Id = identifier;
    }

    public IReadOnlyList<EventSourcedChangeEvent> ChangeEvents { get; init; } = new List<EventSourcedChangeEvent>();

    public bool ClearedChanges { get; private set; }

    public IEnumerable<EventSourcedChangeEvent> LoadedChangeEvents { get; set; } = new List<EventSourcedChangeEvent>();

    public Optional<bool> IsDeleted { get; } = Optional<bool>.None;

    public HydrationProperties Dehydrate()
    {
        throw new NotImplementedException();
    }

    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

    public DateTime LastModifiedAtUtc { get; } = DateTime.UtcNow;

    public IReadOnlyList<IDomainEvent> Events { get; } = new List<IDomainEvent>();

    public Optional<DateTime> LastPersistedAtUtc { get; } = Optional<DateTime>.None;

    public ISingleValueObject<string> Id { get; }

    public Result<Error> HandleStateChanged(IDomainEvent @event)
    {
        throw new NotImplementedException();
    }

    public Result<List<EventSourcedChangeEvent>, Error> GetChanges()
    {
        return ChangeEvents.ToList();
    }

    public Result<Error> ClearChanges()
    {
        ClearedChanges = true;
        return Result.Ok;
    }

    public Result<Error> LoadChanges(IEnumerable<EventSourcedChangeEvent> history,
        IEventSourcedChangeEventMigrator migrator)
    {
        LoadedChangeEvents = history;

        return Result.Ok;
    }

    public Result<Error> RaiseEvent(IDomainEvent @event, bool validate)
    {
        throw new NotImplementedException();
    }
}