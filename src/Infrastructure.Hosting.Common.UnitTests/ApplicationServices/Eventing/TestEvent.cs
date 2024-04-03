using Domain.Common;

namespace Infrastructure.Hosting.Common.UnitTests.ApplicationServices.Eventing;

public class TestEvent : DomainEvent
{
    public TestEvent() : base("arootid")
    {
    }

    public required string Id { get; set; }
}