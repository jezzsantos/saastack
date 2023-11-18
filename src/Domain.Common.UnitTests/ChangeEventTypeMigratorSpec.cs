using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Interfaces.Entities;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Common.UnitTests;

[Trait("Category", "Unit")]
public class ChangeEventTypeMigratorSpec
{
    private readonly Dictionary<string, string> _mappings;
    private readonly ChangeEventTypeMigrator _migrator;

    public ChangeEventTypeMigratorSpec()
    {
        _mappings = new Dictionary<string, string>();
        _migrator = new ChangeEventTypeMigrator(_mappings);
    }

    [Fact]
    public void WhenRehydrateAndTypeKnown_ThenReturnsNewInstance()
    {
        var eventJson = new TestChangeEvent { RootId = "anentityid" }.ToEventJson();
        var result = _migrator.Rehydrate("aneventid", eventJson, typeof(TestChangeEvent).AssemblyQualifiedName!).Value;

        result.Should().BeOfType<TestChangeEvent>();
        result.As<TestChangeEvent>().RootId.Should().Be("anentityid");
    }

    [Fact]
    public void WhenRehydrateAndUnknownType_ThenReturnsError()
    {
        var eventJson = new TestChangeEvent { RootId = "anentityid" }.ToEventJson();

        var result = _migrator.Rehydrate("aneventid", eventJson, "anunknowntype");

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.ChangeEventMigrator_UnknownType.Format("aneventid", "anunknowntype"));
    }

    [Fact]
    public void WhenRehydrateAndUnknownTypeAndMappingStillNotExist_ThenReturnsError()
    {
        _mappings.Add("anunknowntype", "anotherunknowntype");
        var eventJson = new TestChangeEvent { RootId = "anentityid" }.ToEventJson();

        var result = _migrator.Rehydrate("aneventid", eventJson, "anunknowntype");

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.ChangeEventMigrator_UnknownType.Format("aneventid", "anunknowntype"));
    }

    [Fact]
    public void WhenRehydrateAndUnknownTypeAndMappingExists_ThenReturnsNewInstance()
    {
        _mappings.Add("anunknowntype", typeof(TestRenamedChangeEvent).AssemblyQualifiedName!);
        var eventJson = new TestChangeEvent { RootId = "anentityid" }.ToEventJson();
        var result = _migrator.Rehydrate("aneventid", eventJson, "anunknowntype").Value;

        result.Should().BeOfType<TestRenamedChangeEvent>();
        result.As<TestRenamedChangeEvent>().RootId.Should().Be("anentityid");
    }
}

public class TestChangeEvent : IDomainEvent
{
    public string RootId { get; set; } = "anid";

    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
}

public class TestRenamedChangeEvent : IDomainEvent
{
    public string RootId { get; set; } = "anid";

    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
}