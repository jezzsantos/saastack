using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.Entities;

[Trait("Category", "Unit")]
public class DomainEventExtensionsSpec
{
    [Fact]
    public void WhenFromEventJsonWithEmptyJson_ThenReturnsInstance()
    {
        var result = "{}".FromEventJson(typeof(TestEvent));

        result.RootId.Should().BeNull();
        result.OccurredUtc.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void WhenFromEventJsonWithPopulatedJson_ThenReturnsInstance()
    {
        var datum = DateTime.UtcNow;
        var result = new TestEvent
        {
            APropertyValue = "apropertyvalue",
            RootId = "anid",
            OccurredUtc = datum
        }.ToEventJson().FromEventJson(typeof(TestEvent));

        result.Should().BeOfType<TestEvent>();
        result.As<TestEvent>().APropertyValue.Should().Be("apropertyvalue");
        result.RootId.Should().Be("anid");
        result.OccurredUtc.Should().Be(datum);
    }

    [Fact]
    public void WhenToEventJsonWithPopulatedEvent_ThenReturnsJson()
    {
        var datum = DateTime.UtcNow;
        var result = new TestEvent
        {
            APropertyValue = "apropertyvalue",
            RootId = "anid",
            OccurredUtc = datum
        }.ToEventJson();

        result.Should()
            .Be(
                $"{{\"APropertyValue\":\"apropertyvalue\",\"RootId\":\"anid\",\"OccurredUtc\":\"{datum.ToIso8601()}\"}}");
    }

    [Fact]
    public void WhenToVersioned_ThenReturnsEvent()
    {
        var datum = DateTime.UtcNow.ToNearestSecond();

        var result = new TestEvent
        {
            APropertyValue = "apropertyvalue",
            RootId = "anid",
            OccurredUtc = datum
        }.ToVersioned("anid".ToIdentifierFactory(), "anentitytype", 6).Value;

        result.Id.Should().Be("anid".ToId());
        result.LastPersistedAtUtc.Should().BeNull();
        result.Data.Should()
            .Be(
                $"{{\"APropertyValue\":\"apropertyvalue\",\"RootId\":\"anid\",\"OccurredUtc\":\"{datum.ToIso8601()}\"}}");
        result.Metadata.Should()
            .Be(
                "Domain.Common.UnitTests.Entities.TestEvent, Domain.Common.UnitTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        result.Version.Should().Be(6);
        result.EntityType.Should().Be("anentitytype");
        result.EventType.Should().Be(nameof(TestEvent));
    }
}