#if TESTINGONLY
#pragma warning disable CS0618 // Type or member is obsolete
using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.TestingOnly;
using Domain.Interfaces.Entities;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Events.Shared.UnitTests.TestingOnly;

[Trait("Category", "Unit")]
public class HappenedSpec
{
    private readonly IEventSourcedChangeEventMigrator _migrator;

    public HappenedSpec()
    {
        _migrator = new ChangeEventTypeMigrator(new Dictionary<string, string>
        {
            { typeof(Happened).AssemblyQualifiedName!, typeof(HappenedV2).AssemblyQualifiedName! }
        });
    }

    [Fact]
    public void WhenMigrateV1ToV2_ThenMigrates()
    {
        var @event = new Happened("anid".ToId())
        {
            Message1 = "amessage1"
        };

        var result = _migrator.Rehydrate("aneventid", @event);

        result.Should().BeSuccess();
        result.Value.Should().BeOfType<HappenedV2>();
        result.Value.As<HappenedV2>().Message1.Should().BeNull();
        result.Value.As<HappenedV2>().Message2.Should().Be("amessage2");
        result.Value.As<HappenedV2>().Message3.Should().Be("amessage1");
    }

    [Fact]
    public void WhenMigrateV2_ThenMigrates()
    {
        var @event = new HappenedV2("anid".ToId())
        {
            //Message1 = "amessage1",
            Message2 = "amessage2",
            Message3 = "amessage3"
        };

        var result = _migrator.Rehydrate("aneventid", @event);

        result.Should().BeSuccess();
        result.Value.Should().BeOfType<HappenedV2>();
        result.Value.As<HappenedV2>().Message1.Should().BeNull();
        result.Value.As<HappenedV2>().Message2.Should().Be("amessage2");
        result.Value.As<HappenedV2>().Message3.Should().Be("amessage3");
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
#endif