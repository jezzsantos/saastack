using Common;
using Common.Extensions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Interfaces.UnitTests;

[Trait("Category", "Unit")]
public class PersistedEntityMetadataSpec
{
    [Fact]
    public void WhenConstructedWithNoProperties_ThenAssigned()
    {
        var result = new PersistedEntityMetadata();

        result.Types.Should().BeEmpty();
    }

    [Fact]
    public void WhenEmpty_ThenReturnsEmptyMetadata()
    {
        var result = PersistedEntityMetadata.Empty;

        result.Types.Should().BeEmpty();
    }

    [Fact]
    public void WhenConstructedWithProperties_ThenAssigned()
    {
        var result = new PersistedEntityMetadata(new Dictionary<string, Type>
        {
            { "aname1", typeof(string) },
            { "aname2", typeof(int) },
            { "aname3", typeof(DateTime) },
            { "aname4", typeof(int?) },
            { "aname5", typeof(DateTime?) }
        });

        result.Types.Count.Should().Be(5);
        result.Types["aname1"].Should().Be(typeof(string));
        result.Types["aname2"].Should().Be(typeof(int));
        result.Types["aname3"].Should().Be(typeof(DateTime));
        result.Types["aname4"].Should().Be(typeof(int?));
        result.Types["aname5"].Should().Be(typeof(DateTime?));
    }

    [Fact]
    public void WhenFromTypeGeneric_ThenReturnsMetadata()
    {
        var result = PersistedEntityMetadata.FromType<TestCommandDomainEntity>();

        result.Types.Count.Should().Be(17);
        result.Types[nameof(TestCommandDomainEntity.Id)].Should().Be(typeof(string));
        result.Types[nameof(TestCommandDomainEntity.IsDeleted)].Should().Be(typeof(Optional<bool>));
        result.Types[nameof(TestCommandDomainEntity.LastPersistedAtUtc)].Should().Be(typeof(Optional<DateTime>));
        result.Types[nameof(TestCommandDomainEntity.AStringValue)].Should().Be(typeof(string));
        result.Types[nameof(TestCommandDomainEntity.AnIntegerValue)].Should().Be(typeof(int));
        result.Types[nameof(TestCommandDomainEntity.ABooleanValue)].Should().Be(typeof(bool));
        result.Types[nameof(TestCommandDomainEntity.ADateTimeValue)].Should().Be(typeof(DateTime));
        result.Types[nameof(TestCommandDomainEntity.ANullableString)].Should().Be(typeof(string));
        result.Types[nameof(TestCommandDomainEntity.ANullableInteger)].Should().Be(typeof(int?));
        result.Types[nameof(TestCommandDomainEntity.ANullableBoolean)].Should().Be(typeof(bool?));
        result.Types[nameof(TestCommandDomainEntity.ANullableDateTime)].Should().Be(typeof(DateTime?));
        result.Types[nameof(TestCommandDomainEntity.AnOptionalString)].Should().Be(typeof(Optional<string>));
        result.Types[nameof(TestCommandDomainEntity.AnOptionalDateTime)].Should().Be(typeof(Optional<DateTime>));
        result.Types[nameof(TestCommandDomainEntity.AnOptionalNullableString)].Should().Be(typeof(Optional<string?>));
        result.Types[nameof(TestCommandDomainEntity.AnOptionalNullableDateTime)].Should()
            .Be(typeof(Optional<DateTime?>));
        result.Types[nameof(TestCommandDomainEntity.AValueObject)].Should().Be(typeof(TestValueObject));
        result.Types[nameof(TestCommandDomainEntity.AnOptionalValueObject)].Should()
            .Be(typeof(Optional<TestValueObject>));
    }

    [Fact]
    public void WhenFromType_ThenReturnsMetadata()
    {
        var result = PersistedEntityMetadata.FromType(typeof(TestCommandDomainEntity));

        result.Types.Count.Should().Be(17);
        result.Types[nameof(TestCommandDomainEntity.Id)].Should().Be(typeof(string));
        result.Types[nameof(TestCommandDomainEntity.IsDeleted)].Should().Be(typeof(Optional<bool>));
        result.Types[nameof(TestCommandDomainEntity.LastPersistedAtUtc)].Should().Be(typeof(Optional<DateTime>));
        result.Types[nameof(TestCommandDomainEntity.AStringValue)].Should().Be(typeof(string));
        result.Types[nameof(TestCommandDomainEntity.AnIntegerValue)].Should().Be(typeof(int));
        result.Types[nameof(TestCommandDomainEntity.ABooleanValue)].Should().Be(typeof(bool));
        result.Types[nameof(TestCommandDomainEntity.ADateTimeValue)].Should().Be(typeof(DateTime));
        result.Types[nameof(TestCommandDomainEntity.ANullableString)].Should().Be(typeof(string));
        result.Types[nameof(TestCommandDomainEntity.ANullableInteger)].Should().Be(typeof(int?));
        result.Types[nameof(TestCommandDomainEntity.ANullableBoolean)].Should().Be(typeof(bool?));
        result.Types[nameof(TestCommandDomainEntity.ANullableDateTime)].Should().Be(typeof(DateTime?));
        result.Types[nameof(TestCommandDomainEntity.AnOptionalString)].Should().Be(typeof(Optional<string>));
        result.Types[nameof(TestCommandDomainEntity.AnOptionalDateTime)].Should().Be(typeof(Optional<DateTime>));
        result.Types[nameof(TestCommandDomainEntity.AnOptionalNullableString)].Should().Be(typeof(Optional<string?>));
        result.Types[nameof(TestCommandDomainEntity.AnOptionalNullableDateTime)].Should()
            .Be(typeof(Optional<DateTime?>));
        result.Types[nameof(TestCommandDomainEntity.AValueObject)].Should().Be(typeof(TestValueObject));
        result.Types[nameof(TestCommandDomainEntity.AnOptionalValueObject)].Should()
            .Be(typeof(Optional<TestValueObject>));
    }

    [Fact]
    public void WhenGetPropertyTypeAndNotExistsAndThrows_ThenThrows()
    {
        var metadata = PersistedEntityMetadata.FromType(typeof(TestDto));

        metadata.Invoking(x => x.GetPropertyType("anunknownproperty"))
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                Resources.PersistedEntityMetadata_GetPropertyType_NoTypeForProperty.Format("anunknownproperty"));
    }

    [Fact]
    public void WhenGetPropertyTypeAndNotExistsAndNotThrow_ThenReturnsNull()
    {
        var metadata = PersistedEntityMetadata.FromType(typeof(TestDto));

        var result = metadata.GetPropertyType("anunknownproperty", false);

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetPropertyType_ThenReturnsType()
    {
        var metadata = PersistedEntityMetadata.FromType(typeof(TestDto));

        var result = metadata.GetPropertyType(nameof(TestDto.AStringValue));

        ((object)result).Should().Be(typeof(string));
    }

    [Fact]
    public void WhenHasTypeAndNotExists_ThenReturnsFalse()
    {
        var metadata = PersistedEntityMetadata.FromType(typeof(TestDto));

        var result = metadata.HasType("anunknownproperty");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasTypeAndExists_ThenReturnsTrue()
    {
        var metadata = PersistedEntityMetadata.FromType(typeof(TestDto));

        var result = metadata.HasType(nameof(TestDto.AStringValue));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenChangeTypeAndNotExists_ThenAddsType()
    {
        var metadata = PersistedEntityMetadata.FromType(typeof(TestDto));

        metadata.AddOrUpdate("anunknownproperty", typeof(string));

        metadata.Types[nameof(TestDto.AStringValue)].Should().Be(typeof(string));
    }

    [Fact]
    public void WhenChangeTypeAndExists_ThenUpdatesType()
    {
        var metadata = PersistedEntityMetadata.FromType(typeof(TestDto));

        metadata.AddOrUpdate(nameof(TestDto.AStringValue), typeof(int));

        metadata.Types[nameof(TestDto.AStringValue)].Should().Be(typeof(int));
    }
}