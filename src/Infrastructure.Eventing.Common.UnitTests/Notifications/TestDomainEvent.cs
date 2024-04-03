using Domain.Common;
using Infrastructure.Eventing.Common.Notifications;

namespace Infrastructure.Eventing.Common.UnitTests.Notifications;

public class TestDomainEvent : DomainEvent
{
    public TestDomainEvent() : base("arootid")
    {
    }
}

public class TestIntegrationEvent : IntegrationEvent
{
    public TestIntegrationEvent() : base("arootid")
    {
    }

    public TestIntegrationEvent(string rootId) : base(rootId)
    {
    }
}