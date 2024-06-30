using Common;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using QueryAny;

namespace Infrastructure.Hosting.Common.UnitTests.ApplicationServices.Eventing;

[EntityName("acontainername")]
public class TestEventingAggregateRoot : IEventingAggregateRoot, IDehydratableAggregateRoot
{
    private readonly List<EventSourcedChangeEvent> _changeEvents = new();

    public TestEventingAggregateRoot(Identifier identifier)
    {
        Id = identifier;
    }

    public bool ClearedChanges { get; private set; }

    public IEnumerable<EventSourcedChangeEvent> LoadedChangeEvents { get; set; } = new List<EventSourcedChangeEvent>();

    public HydrationProperties Dehydrate()
    {
        throw new NotImplementedException();
    }

    public Optional<bool> IsDeleted { get; } = Optional<bool>.None;

    public Result<Error> ClearChanges()
    {
        ClearedChanges = true;
        return Result.Ok;
    }

    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

    public IReadOnlyList<IDomainEvent> Events { get; } = new List<IDomainEvent>();

    public Result<List<EventSourcedChangeEvent>, Error> GetChanges()
    {
        return _changeEvents.ToList();
    }

    public Result<Error> HandleStateChanged(IDomainEvent @event)
    {
        throw new NotImplementedException();
    }

    public ISingleValueObject<string> Id { get; }

    public DateTime LastModifiedAtUtc { get; } = DateTime.UtcNow;

    public Optional<DateTime> LastPersistedAtUtc { get; } = Optional<DateTime>.None;

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

    public void AddEvents(params TestEvent[] events)
    {
        var counter = 0;
        foreach (var @event in events)
        {
            _changeEvents.Add(@event.ToVersioned(new FixedIdentifierFactory(@event.Id),
                nameof(TestEventingAggregateRoot),
                ++counter).Value);
        }
    }
}