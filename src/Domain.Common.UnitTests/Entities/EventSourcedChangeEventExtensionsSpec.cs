using Domain.Common.Entities;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Domain.Common.UnitTests.Entities;

[Trait("Category", "Unit")]
public class EventSourcedChangeEventExtensionsSpec
{
    [Fact]
    public void WhenToEvent_ThenReturnsEvent()
    {
        var id = new Mock<ISingleValueObject<string>>();
        var @event = EventSourcedChangeEvent.Create(id.Object, "anentitytype", "aneventtype", "json", "metadata", 999);
        var migrator = new Mock<IEventSourcedChangeEventMigrator>();
        migrator.Setup(m => m.Rehydrate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new TestEvent());

        var result = @event.ToEvent(migrator.Object).Value;

        result.Should().BeOfType<TestEvent>();
    }
}