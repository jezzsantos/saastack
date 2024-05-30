using Domain.Common;

namespace Infrastructure.Worker.Api.IntegrationTests;

public class TestDomainEvent : DomainEvent
{
    public TestDomainEvent() : base("arootid")
    {
    }
}