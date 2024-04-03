using Domain.Common;

namespace Infrastructure.Eventing.Common.UnitTests.Projections;

public class TestEvent : DomainEvent
{
    public TestEvent() : base("arootid")
    {
    }
}