using Application.Persistence.Interfaces;
using Common;
using Domain.Interfaces.Entities;

namespace Infrastructure.Hosting.Common.UnitTests.ApplicationServices.Eventing.Projections;

public class TestProjection : IReadModelProjection
{
    private readonly List<IDomainEvent> _projectedEvents = new();

    public IDomainEvent[] ProjectedEvents => _projectedEvents.ToArray();

    public Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent, CancellationToken cancellationToken)
    {
        _projectedEvents.Add(changeEvent);

        return Task.FromResult<Result<bool, Error>>(true);
    }

    public Type RootAggregateType => typeof(TestEventingAggregateRoot);
}