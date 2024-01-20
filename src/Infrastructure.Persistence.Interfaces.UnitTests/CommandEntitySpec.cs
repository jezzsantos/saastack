using Application.Persistence.Interfaces;
using Common;
using Domain.Interfaces;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Interfaces.UnitTests;

[Trait("Category", "Unit")]
public class CommandEntitySpec
{
    [Fact]
    public void WhenConstructed_ThenPropertiesAndMetadataIsAssigned()
    {
        var result = new CommandEntity("anid");

        result.Id.Should().BeSome("anid");
        result.IsDeleted.Should().BeNone();
        result.LastPersistedAtUtc.Should().BeNone();
        result.Metadata.Types.Count.Should().Be(3);
        result.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        result.Properties.Count.Should().Be(3);
        result.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        result.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        result.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
    }

    [Fact]
    public void WhenFromType_ThenPropertiesAndMetadataIsAssigned()
    {
        var datum = DateTime.UtcNow;
        var result = CommandEntity.FromType(new TestDto
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum
        });

        result.Id.Should().BeSome("anid");
        result.IsDeleted.Should().BeNone();
        result.LastPersistedAtUtc.Should().BeNone();
        result.Metadata.Types.Count.Should().Be(11);
        result.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(Optional<string>));
        result.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        result.Metadata.Types[nameof(TestDto.AStringValue)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(TestDto.AnIntegerValue)].Should().Be(typeof(int));
        result.Metadata.Types[nameof(TestDto.ABooleanValue)].Should().Be(typeof(bool));
        result.Metadata.Types[nameof(TestDto.ADateTimeValue)].Should().Be(typeof(DateTime));
        result.Metadata.Types[nameof(TestDto.ANullableString)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(TestDto.ANullableInteger)].Should().Be(typeof(int?));
        result.Metadata.Types[nameof(TestDto.ANullableBoolean)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(TestDto.ANullableDateTime)].Should().Be(typeof(DateTime?));
        result.Properties.Count.Should().Be(11);
        result.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        result.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        result.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        result.Properties[nameof(TestDto.AStringValue)].Should().BeSome("avalue");
        result.Properties[nameof(TestDto.AnIntegerValue)].Should().BeSome(1);
        result.Properties[nameof(TestDto.ABooleanValue)].Should().BeSome(true);
        result.Properties[nameof(TestDto.ADateTimeValue)].Should().BeSome(datum);
        result.Properties[nameof(TestDto.ANullableString)].Should().BeNone();
        result.Properties[nameof(TestDto.ANullableInteger)].Should().BeNone();
        result.Properties[nameof(TestDto.ANullableBoolean)].Should().BeNone();
        result.Properties[nameof(TestDto.ANullableDateTime)].Should().BeNone();
    }

    [Fact]
    public void WhenFromDto_ThenPropertiesAndMetadataIsAssigned()
    {
        var datum = DateTime.UtcNow;
        var result = CommandEntity.FromDto(new TestDto
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum
        });

        result.Id.Should().Be("anid");
        result.IsDeleted.Should().BeNone();
        result.LastPersistedAtUtc.Should().BeNone();
        result.Metadata.Types.Count.Should().Be(11);
        result.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(Optional<string>));
        result.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        result.Metadata.Types[nameof(TestDto.AStringValue)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(TestDto.AnIntegerValue)].Should().Be(typeof(int));
        result.Metadata.Types[nameof(TestDto.ABooleanValue)].Should().Be(typeof(bool));
        result.Metadata.Types[nameof(TestDto.ADateTimeValue)].Should().Be(typeof(DateTime));
        result.Metadata.Types[nameof(TestDto.ANullableString)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(TestDto.ANullableInteger)].Should().Be(typeof(int?));
        result.Metadata.Types[nameof(TestDto.ANullableBoolean)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(TestDto.ANullableDateTime)].Should().Be(typeof(DateTime?));
        result.Properties.Count.Should().Be(11);
        result.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        result.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        result.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        result.Properties[nameof(TestDto.AStringValue)].Should().BeSome("avalue");
        result.Properties[nameof(TestDto.AnIntegerValue)].Should().BeSome(1);
        result.Properties[nameof(TestDto.ABooleanValue)].Should().BeSome(true);
        result.Properties[nameof(TestDto.ADateTimeValue)].Should().BeSome(datum);
        result.Properties[nameof(TestDto.ANullableString)].Should().BeNone();
        result.Properties[nameof(TestDto.ANullableInteger)].Should().BeNone();
        result.Properties[nameof(TestDto.ANullableBoolean)].Should().BeNone();
        result.Properties[nameof(TestDto.ANullableDateTime)].Should().BeNone();
    }

    [Fact]
    public void WhenFromCommandEntityWithEmptyDictionaryAndOtherCommandEntityAsDescriptor_ThenThrows()
    {
        var datum = DateTime.UtcNow;
        var other = CommandEntity.FromType(new TestDto
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum
        });

        FluentActions.Invoking(() => CommandEntity.FromCommandEntity(new HydrationProperties(), other))
            .Should().Throw<InvalidOperationException>()
            .WithMessage(Resources.CommandEntity_FromProperties_NoId);
    }

    [Fact]
    public void WhenFromCommandEntityWithIdOnlyAndOtherCommandEntityAsDescriptor_ThenReturnsEntity()
    {
        var datum = DateTime.UtcNow;
        var otherInstance = CommandEntity.FromType(new TestDto
        {
            Id = "anotherid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum
        });

        var result = CommandEntity.FromCommandEntity(new HydrationProperties
        {
            { nameof(IHasIdentity.Id), "anid" }
        }, otherInstance);

        result.Id.Should().Be("anid");
        result.IsDeleted.Should().BeNone();
        result.LastPersistedAtUtc.Should().BeNone();
        result.Metadata.Types.Count.Should().Be(3);
        result.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(Optional<string>));
        result.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        result.Properties.Count.Should().Be(3);
        result.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        result.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        result.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
    }

    [Fact]
    public void
        WhenFromCommandEntityWithNonMatchingPropertiesAndOtherCommandEntityAsDescriptor_ThenReturnsEntityWithBareMinimumProperties()
    {
        var datum = DateTime.UtcNow;
        var otherInstance = CommandEntity.FromType(new TestDto
        {
            Id = "anotherid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum
        });

        var result = CommandEntity.FromCommandEntity(new HydrationProperties
        {
            { nameof(IHasIdentity.Id), "anid" },
            { "AnUnknownProperty", "avalue" }
        }, otherInstance);

        result.Id.Should().Be("anid");
        result.IsDeleted.Should().BeNone();
        result.LastPersistedAtUtc.Should().BeNone();
        result.Metadata.Types.Count.Should().Be(3);
        result.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(Optional<string>));
        result.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        result.Properties.Count.Should().Be(3);
        result.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        result.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        result.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
    }

    [Fact]
    public void
        WhenFromCommandEntityWithMatchingPropertiesAndOtherCommandEntityAsDescriptor_ThenReturnsEntityWithOnlyMatchingProperties()
    {
        var datum = DateTime.UtcNow;
        var otherInstance = CommandEntity.FromType(new TestDto
        {
            Id = "anotherid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum
        });

        var result = CommandEntity.FromCommandEntity(new HydrationProperties
        {
            { nameof(IHasIdentity.Id), "anid" },
            { nameof(TestDto.AStringValue), "avalue" },
            { nameof(TestDto.ANullableString), (string?)null }
        }, otherInstance);

        result.Id.Should().Be("anid");
        result.IsDeleted.Should().BeNone();
        result.LastPersistedAtUtc.Should().BeNone();
        result.Metadata.Types.Count.Should().Be(5);
        result.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(Optional<string>));
        result.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        result.Metadata.Types[nameof(TestDto.AStringValue)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(TestDto.ANullableString)].Should().Be(typeof(string));
        result.Properties.Count.Should().Be(5);
        result.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        result.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        result.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        result.Properties[nameof(TestDto.AStringValue)].Should().BeSome("avalue");
        result.Properties[nameof(TestDto.ANullableString)].Should().BeNone();
    }

    [Fact]
    public void
        WhenFromCommandEntityWithMatchingPropertiesAndOtherMetadata_ThenReturnsEntityWithOnlyMatchingProperties()
    {
        var datum = DateTime.UtcNow;
        var otherMetadata = CommandEntity.FromType(new TestDto
        {
            Id = "anotherid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum
        }).Metadata;

        var result = CommandEntity.FromCommandEntity(new HydrationProperties
        {
            { nameof(IHasIdentity.Id), "anid" },
            { nameof(TestDto.AStringValue), "avalue" },
            { nameof(TestDto.ANullableString), (string?)null }
        }, otherMetadata);

        result.Id.Should().Be("anid");
        result.IsDeleted.Should().BeNone();
        result.LastPersistedAtUtc.Should().BeNone();
        result.Metadata.Types.Count.Should().Be(5);
        result.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(Optional<string>));
        result.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        result.Metadata.Types[nameof(TestDto.AStringValue)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(TestDto.ANullableString)].Should().Be(typeof(string));
        result.Properties.Count.Should().Be(5);
        result.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        result.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        result.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        result.Properties[nameof(TestDto.AStringValue)].Should().BeSome("avalue");
        result.Properties[nameof(TestDto.ANullableString)].Should().BeNone();
    }

    [Fact]
    public void WhenFromDomainEntityAndNoIdInDehydrationProperties_ThenThrows()
    {
        var datum = DateTime.UtcNow;
        var domain = new TestCommandDomainEntity
        {
            Id = null!,
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum,
            AnOptionalString = "anoptionalvalue",
            AnOptionalDateTime = datum,
            AnOptionalNullableString = "anoptionalvalue",
            AnOptionalNullableDateTime = datum,
            AValueObject = TestValueObject.Create("avalueobject").Value,
            AnOptionalValueObject = TestValueObject.Create("avalueobject").Value
        };

        FluentActions.Invoking(() => CommandEntity.FromDomainEntity(domain))
            .Should().Throw<InvalidOperationException>()
            .WithMessage(Resources.CommandEntity_FromProperties_NoId);
    }

    [Fact]
    public void WhenFromDomainEntity_ThenReturnsEntityWithDehydratedProperties()
    {
        var datum = DateTime.UtcNow;
        var entity = new TestCommandDomainEntity
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum,
            AnOptionalString = "anoptionalvalue",
            AnOptionalDateTime = datum,
            AnOptionalNullableString = "anoptionalvalue",
            AnOptionalNullableDateTime = datum,
            AValueObject = TestValueObject.Create("avalueobject").Value,
            AnOptionalValueObject = TestValueObject.Create("anoptionalvalueobject").Value
        };

        var result = CommandEntity.FromDomainEntity(entity);

        result.Id.Should().Be("anid");
        result.IsDeleted.Should().BeNone();
        result.LastPersistedAtUtc.Should().BeNone();
        result.Metadata.Types.Count.Should().Be(13);
        result.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        result.Metadata.Types[nameof(TestCommandDomainEntity.AStringValue)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(TestCommandDomainEntity.AnIntegerValue)].Should().Be(typeof(int));
        result.Metadata.Types[nameof(TestCommandDomainEntity.ABooleanValue)].Should().Be(typeof(bool));
        result.Metadata.Types[nameof(TestCommandDomainEntity.ADateTimeValue)].Should().Be(typeof(DateTime));
        result.Metadata.Types[nameof(TestCommandDomainEntity.AnOptionalString)].Should().Be(typeof(Optional<string>));
        result.Metadata.Types[nameof(TestCommandDomainEntity.AnOptionalDateTime)].Should()
            .Be(typeof(Optional<DateTime>));
        result.Metadata.Types[nameof(TestCommandDomainEntity.AnOptionalNullableString)].Should()
            .Be(typeof(Optional<string?>));
        result.Metadata.Types[nameof(TestCommandDomainEntity.AnOptionalNullableDateTime)].Should()
            .Be(typeof(Optional<DateTime?>));
        result.Metadata.Types[nameof(TestCommandDomainEntity.AValueObject)].Should().Be(typeof(TestValueObject));
        result.Metadata.Types[nameof(TestCommandDomainEntity.AnOptionalValueObject)].Should()
            .Be(typeof(Optional<TestValueObject>));
        result.Properties.Count.Should().Be(13);
        result.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        result.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        result.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        result.Properties[nameof(TestCommandDomainEntity.AStringValue)].Should().BeSome("avalue");
        result.Properties[nameof(TestCommandDomainEntity.AnIntegerValue)].Should().BeSome(1);
        result.Properties[nameof(TestCommandDomainEntity.ABooleanValue)].Should().BeSome(true);
        result.Properties[nameof(TestCommandDomainEntity.ADateTimeValue)].Should().BeSome(datum);
        result.Properties[nameof(TestCommandDomainEntity.AnOptionalString)].Should().BeSome("anoptionalvalue");
        result.Properties[nameof(TestCommandDomainEntity.AnOptionalDateTime)].Should().BeSome(datum);
        result.Properties[nameof(TestCommandDomainEntity.AnOptionalNullableString)].Should().BeSome("anoptionalvalue");
        result.Properties[nameof(TestCommandDomainEntity.AnOptionalNullableDateTime)].Should().BeSome(datum);
        result.Properties[nameof(TestCommandDomainEntity.AValueObject)].Should().BeSome("avalueobject");
        result.Properties[nameof(TestCommandDomainEntity.AnOptionalValueObject)].Should()
            .BeSome("anoptionalvalueobject");
    }

    [Fact]
    public void WhenAddWithNoneValue_ThenAddsNullValueProperty()
    {
        var entity = new CommandEntity("anid");

        entity.Add("aname", Optional<string>.None);

        entity.Id.Should().BeSome("anid");
        entity.IsDeleted.Should().BeNone();
        entity.LastPersistedAtUtc.Should().BeNone();
        entity.Metadata.Types.Count.Should().Be(4);
        entity.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        entity.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        entity.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        entity.Metadata.Types["aname"].Should().Be(typeof(string));
        entity.Properties.Count.Should().Be(4);
        entity.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        entity.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        entity.Properties["aname"].Should().BeNone();
    }

    [Fact]
    public void WhenAddWithPrimitiveValue_ThenAddsProperty()
    {
        var entity = new CommandEntity("anid");

        entity.Add("aname", "avalue");

        entity.Id.Should().BeSome("anid");
        entity.IsDeleted.Should().BeNone();
        entity.LastPersistedAtUtc.Should().BeNone();
        entity.Metadata.Types.Count.Should().Be(4);
        entity.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        entity.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        entity.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        entity.Metadata.Types["aname"].Should().Be(typeof(string));
        entity.Properties.Count.Should().Be(4);
        entity.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        entity.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        entity.Properties["aname"].Should().BeSome("avalue");
    }

    [Fact]
    public void WhenAddWithValueObjectValue_ThenUpdatesProperty()
    {
        var entity = new CommandEntity("anid");

        entity.Add("aname", TestValueObject.Create("avalue").Value);

        entity.Id.Should().BeSome("anid");
        entity.IsDeleted.Should().BeNone();
        entity.LastPersistedAtUtc.Should().BeNone();
        entity.Metadata.Types.Count.Should().Be(4);
        entity.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        entity.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        entity.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        entity.Metadata.Types["aname"].Should().Be(typeof(TestValueObject));
        entity.Properties.Count.Should().Be(4);
        entity.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        entity.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        entity.Properties["aname"].Should().BeSome("avalue");
    }

    [Fact]
    public void WhenAddAndValueAlreadyExists_ThenUpdatesProperty()
    {
        var entity = new CommandEntity("anid");

        entity.Add("aname", "avalue1");
        entity.Add("aname", "avalue2");

        entity.Id.Should().BeSome("anid");
        entity.IsDeleted.Should().BeNone();
        entity.LastPersistedAtUtc.Should().BeNone();
        entity.Metadata.Types.Count.Should().Be(4);
        entity.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        entity.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        entity.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        entity.Metadata.Types["aname"].Should().Be(typeof(string));
        entity.Properties.Count.Should().Be(4);
        entity.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        entity.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        entity.Properties["aname"].Should().BeSome("avalue2");
    }

    [Fact]
    public void WhenAddWithNewDataTypeAndValueAlreadyExists_ThenUpdatesProperty()
    {
        var entity = new CommandEntity("anid");

        entity.Add("aname", "avalue1");
        entity.Add("aname", 1);

        entity.Id.Should().BeSome("anid");
        entity.IsDeleted.Should().BeNone();
        entity.LastPersistedAtUtc.Should().BeNone();
        entity.Metadata.Types.Count.Should().Be(4);
        entity.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        entity.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        entity.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        entity.Metadata.Types["aname"].Should().Be(typeof(int));
        entity.Properties.Count.Should().Be(4);
        entity.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        entity.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        entity.Properties["aname"].Should().BeSome(1);
    }

    [Fact]
    public void WhenToDto_ThenReturnsInstance()
    {
        var datum = DateTime.UtcNow;
        var entity = CommandEntity.FromType(new TestDto
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum
        });

        var result = entity.ToDto<TestDto>();

        result.Id.Should().Be("anid");
        result.AStringValue.Should().Be("avalue");
        result.AnIntegerValue.Should().Be(1);
        result.ABooleanValue.Should().Be(true);
        result.ADateTimeValue.Should().Be(datum);
        result.ANullableString.Should().BeNull();
        result.ANullableInteger.Should().BeNull();
        result.ANullableBoolean.Should().BeNull();
        result.ANullableDateTime.Should().BeNull();
    }

    [Fact]
    public void WhenToToReadModelDtoWithDefaultValues_ThenReturnsInstance()
    {
        var entity = CommandEntity.FromDomainEntity(new TestCommandDomainEntity
        {
            Id = "anid",
            AStringValue = null!,
            AnIntegerValue = 0,
            ABooleanValue = false,
            ADateTimeValue = DateTime.MinValue,
            AnOptionalString = null,
            AnOptionalDateTime = DateTime.MinValue,
            AnOptionalNullableString = null,
            AnOptionalNullableDateTime = null,
            AValueObject = null!,
            AnOptionalValueObject = null
        });
        var domainFactory = new Mock<IDomainFactory>();

        var result = entity.ToReadModelDto<TestReadModel>(domainFactory.Object);

        result.Id.Should().Be("anid");
        result.AStringValue.Should().BeNull();
        result.AnIntegerValue.Should().Be(0);
        result.ABooleanValue.Should().Be(false);
        result.ADateTimeValue.Should().Be(DateTime.MinValue);
        result.ANullableString.Should().BeNull();
        result.ANullableInteger.Should().BeNull();
        result.ANullableBoolean.Should().BeNull();
        result.ANullableDateTime.Should().BeNull();
        result.AnOptionalString.Should().BeNone();
        result.AnOptionalDateTime.Should().BeSome(DateTime.MinValue);
        result.AnOptionalNullableString.Should().BeNone();
        result.AnOptionalNullableDateTime.Should().BeNone();
        result.AValueObject.Should().BeNull();
        result.AnOptionalValueObject.Should().BeNone();
        domainFactory.Verify(df => df.RehydrateValueObject(typeof(TestValueObject), It.IsAny<string>()), Times.Never);
        domainFactory.Verify(df => df.RehydrateValueObject(typeof(Optional<TestValueObject>), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void WhenToToReadModelDto_ThenReturnsInstance()
    {
        var datum = DateTime.UtcNow;
        var valueObject = TestValueObject.Create("avalueobject").Value;
        var optionalValueObject = TestValueObject.Create("anoptionalvalueobject").Value;
        var entity = CommandEntity.FromDomainEntity(new TestCommandDomainEntity
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum,
            AnOptionalString = "anoptionalvalue",
            AnOptionalDateTime = datum,
            AnOptionalNullableString = "anoptionalvalue",
            AnOptionalNullableDateTime = datum,
            AValueObject = valueObject,
            AnOptionalValueObject = optionalValueObject
        });
        var domainFactory = new Mock<IDomainFactory>();
        domainFactory.Setup(df => df.RehydrateValueObject(typeof(TestValueObject), It.IsAny<string>()))
            .Returns(valueObject);
        domainFactory.Setup(df => df.RehydrateValueObject(typeof(Optional<TestValueObject>), It.IsAny<string>()))
            .Returns(optionalValueObject);

        var result = entity.ToReadModelDto<TestReadModel>(domainFactory.Object);

        result.Id.Should().Be("anid");
        result.AStringValue.Should().Be("avalue");
        result.AnIntegerValue.Should().Be(1);
        result.ABooleanValue.Should().Be(true);
        result.ADateTimeValue.Should().Be(datum);
        result.ANullableString.Should().BeNull();
        result.ANullableInteger.Should().BeNull();
        result.ANullableBoolean.Should().BeNull();
        result.ANullableDateTime.Should().BeNull();
        result.AnOptionalString.Should().BeSome("anoptionalvalue");
        result.AnOptionalDateTime.Should().BeSome(datum);
        result.AnOptionalNullableString.Should().BeSome("anoptionalvalue");
        result.AnOptionalNullableDateTime.Should().BeSome(datum);
        result.AValueObject.Should().Be(valueObject);
        result.AnOptionalValueObject.Should().Be(optionalValueObject);
        domainFactory.Verify(df => df.RehydrateValueObject(typeof(TestValueObject), "avalueobject"));
        domainFactory.Verify(df => df.RehydrateValueObject(typeof(Optional<TestValueObject>), "anoptionalvalueobject"));
    }

    [Fact]
    public void WhenToToQueryEntity_ThenReturnsInstance()
    {
        var datum = DateTime.UtcNow;
        var valueObject = TestValueObject.Create("avalueobject").Value;
        var optionalValueObject = TestValueObject.Create("anoptionalvalueobject").Value;
        var entity = CommandEntity.FromDomainEntity(new TestCommandDomainEntity
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum,
            AnOptionalString = "anoptionalvalue",
            AnOptionalDateTime = datum,
            AnOptionalNullableString = "anoptionalvalue",
            AnOptionalNullableDateTime = datum,
            AValueObject = valueObject,
            AnOptionalValueObject = optionalValueObject
        });
        var domainFactory = new Mock<IDomainFactory>();
        domainFactory.Setup(df => df.RehydrateValueObject(It.IsAny<Type>(), It.IsAny<string>()))
            .Returns(valueObject);

        var result = entity.ToQueryEntity<TestDto>(domainFactory.Object);

        result.Id.Should().Be("anid");
        result.AStringValue.Should().Be("avalue");
        result.AnIntegerValue.Should().Be(1);
        result.ABooleanValue.Should().Be(true);
        result.ADateTimeValue.Should().Be(datum);
        result.ANullableString.Should().BeNull();
        result.ANullableInteger.Should().BeNull();
        result.ANullableBoolean.Should().BeNull();
        result.ANullableDateTime.Should().BeNull();
        domainFactory.Verify(df => df.RehydrateValueObject(typeof(TestValueObject), "avalueobject"));
    }

    [Fact]
    public void WhenToDomainEntityForAggregateRoot_ThenReturnsInstance()
    {
        var datum = DateTime.UtcNow;
        var valueObject = TestValueObject.Create("avalueobject").Value;
        var optionalValueObject = TestValueObject.Create("anoptionalvalueobject").Value;
        var entity = CommandEntity.FromDomainEntity(new TestCommandDomainAggregateRoot
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum,
            AnOptionalString = "anoptionalvalue",
            AnOptionalDateTime = datum,
            AnOptionalNullableString = "anoptionalvalue",
            AnOptionalNullableDateTime = datum,
            AValueObject = valueObject,
            AnOptionalValueObject = optionalValueObject
        });
        var rehydratedEntity = new TestCommandDomainAggregateRoot
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum,
            AnOptionalString = "anoptionalvalue",
            AnOptionalDateTime = datum,
            AnOptionalNullableString = "anoptionalvalue",
            AnOptionalNullableDateTime = datum,
            AValueObject = valueObject,
            AnOptionalValueObject = optionalValueObject
        };
        var domainFactory = new Mock<IDomainFactory>();
        domainFactory.Setup(df => df.RehydrateValueObject(typeof(TestValueObject), It.IsAny<string>()))
            .Returns(valueObject);
        domainFactory.Setup(df => df.RehydrateValueObject(typeof(Optional<TestValueObject>), It.IsAny<string>()))
            .Returns(optionalValueObject);
        domainFactory.Setup(df =>
                df.RehydrateAggregateRoot(typeof(TestCommandDomainAggregateRoot), It.IsAny<HydrationProperties>()))
            .Returns(rehydratedEntity);

        var result = entity.ToDomainEntity<TestCommandDomainAggregateRoot>(domainFactory.Object);

        result.Should().Be(rehydratedEntity);
        domainFactory.Verify(df => df.RehydrateValueObject(typeof(TestValueObject), "avalueobject"));
        domainFactory.Verify(df => df.RehydrateValueObject(typeof(Optional<TestValueObject>), "anoptionalvalueobject"));
        domainFactory.Verify(df => df.RehydrateAggregateRoot(typeof(TestCommandDomainAggregateRoot),
            It.Is<HydrationProperties>(
                dic =>
                    dic.Count == 13
                    && (string)dic[nameof(PersistedEntity.Id)].As<Optional<object>>().ValueOrDefault! == "anid"
                    && dic[nameof(PersistedEntity.IsDeleted)].As<Optional<object>>().ValueOrDefault == null
                    && dic[nameof(PersistedEntity.LastPersistedAtUtc)].As<Optional<object>>().ValueOrDefault == null
                    && (string)dic[nameof(TestCommandDomainEntity.AStringValue)].As<Optional<object>>().ValueOrDefault!
                    == "avalue"
                    && (int)dic[nameof(TestCommandDomainEntity.AnIntegerValue)].As<Optional<object>>().ValueOrDefault!
                    == 1
                    && (bool)dic[nameof(TestCommandDomainEntity.ABooleanValue)].As<Optional<object>>().ValueOrDefault!
                    == true
                    && (DateTime)dic[nameof(TestCommandDomainEntity.ADateTimeValue)].As<Optional<object>>()
                        .ValueOrDefault! == datum
                    && (Optional<string>)dic[nameof(TestCommandDomainEntity.AnOptionalString)].As<Optional<object>>()
                        .ValueOrDefault! == "anoptionalvalue"
                    && (Optional<DateTime>)dic[nameof(TestCommandDomainEntity.AnOptionalDateTime)]
                        .As<Optional<object>>()
                        .ValueOrDefault! == datum
                    && (Optional<string?>)dic[nameof(TestCommandDomainEntity.AnOptionalNullableString)]
                        .As<Optional<object>>()
                        .ValueOrDefault! == "anoptionalvalue"
                    && (Optional<DateTime?>)dic[nameof(TestCommandDomainEntity.AnOptionalNullableDateTime)]
                        .As<Optional<object>>()
                        .ValueOrDefault! == (DateTime?)datum
                    && (TestValueObject)dic[nameof(TestCommandDomainEntity.AValueObject)].As<Optional<object>>()
                        .ValueOrDefault! == valueObject
                    && (Optional<TestValueObject>)dic[nameof(TestCommandDomainEntity.AnOptionalValueObject)]
                        .As<Optional<object>>().ValueOrDefault! == optionalValueObject
            )));
        domainFactory.Verify(df => df.RehydrateEntity(It.IsAny<Type>(), It.IsAny<HydrationProperties>()), Times.Never);
    }

    [Fact]
    public void WhenToDomainEntityForEntity_ThenReturnsInstance()
    {
        var datum = DateTime.UtcNow;
        var valueObject = TestValueObject.Create("avalueobject").Value;
        var optionalValueObject = TestValueObject.Create("anoptionalvalueobject").Value;
        var entity = CommandEntity.FromDomainEntity(new TestCommandDomainEntity
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum,
            AnOptionalString = "anoptionalvalue",
            AnOptionalDateTime = datum,
            AnOptionalNullableString = "anoptionalvalue",
            AnOptionalNullableDateTime = datum,
            AValueObject = valueObject,
            AnOptionalValueObject = optionalValueObject
        });
        var rehydratedEntity = new TestCommandDomainEntity
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum,
            AnOptionalString = "anoptionalvalue",
            AnOptionalDateTime = datum,
            AnOptionalNullableString = "anoptionalvalue",
            AnOptionalNullableDateTime = datum,
            AValueObject = valueObject,
            AnOptionalValueObject = optionalValueObject
        };
        var domainFactory = new Mock<IDomainFactory>();
        domainFactory.Setup(df => df.RehydrateValueObject(typeof(TestValueObject), It.IsAny<string>()))
            .Returns(valueObject);
        domainFactory.Setup(df => df.RehydrateValueObject(typeof(Optional<TestValueObject>), It.IsAny<string>()))
            .Returns(optionalValueObject);
        domainFactory.Setup(df =>
                df.RehydrateEntity(typeof(TestCommandDomainEntity), It.IsAny<HydrationProperties>()))
            .Returns(rehydratedEntity);

        var result = entity.ToDomainEntity<TestCommandDomainEntity>(domainFactory.Object);

        result.Should().Be(rehydratedEntity);
        domainFactory.Verify(df => df.RehydrateValueObject(typeof(TestValueObject), "avalueobject"));
        domainFactory.Verify(df => df.RehydrateValueObject(typeof(Optional<TestValueObject>), "anoptionalvalueobject"));
        domainFactory.Verify(df => df.RehydrateEntity(typeof(TestCommandDomainEntity), It.Is<HydrationProperties>(
            dic =>
                dic.Count == 13
                && (string)dic[nameof(PersistedEntity.Id)].As<Optional<object>>().ValueOrDefault! == "anid"
                && dic[nameof(PersistedEntity.IsDeleted)].As<Optional<object>>().ValueOrDefault == null
                && dic[nameof(PersistedEntity.LastPersistedAtUtc)].As<Optional<object>>().ValueOrDefault == null
                && (string)dic[nameof(TestCommandDomainEntity.AStringValue)].As<Optional<object>>().ValueOrDefault!
                == "avalue"
                && (int)dic[nameof(TestCommandDomainEntity.AnIntegerValue)].As<Optional<object>>().ValueOrDefault! == 1
                && (bool)dic[nameof(TestCommandDomainEntity.ABooleanValue)].As<Optional<object>>().ValueOrDefault!
                == true
                && (DateTime)dic[nameof(TestCommandDomainEntity.ADateTimeValue)].As<Optional<object>>().ValueOrDefault!
                == datum
                && (Optional<string>)dic[nameof(TestCommandDomainEntity.AnOptionalString)].As<Optional<object>>()
                    .ValueOrDefault! == "anoptionalvalue"
                && (Optional<DateTime>)dic[nameof(TestCommandDomainEntity.AnOptionalDateTime)].As<Optional<object>>()
                    .ValueOrDefault! == datum
                && (Optional<string?>)dic[nameof(TestCommandDomainEntity.AnOptionalNullableString)]
                    .As<Optional<object>>()
                    .ValueOrDefault! == "anoptionalvalue"
                && (Optional<DateTime?>)dic[nameof(TestCommandDomainEntity.AnOptionalNullableDateTime)]
                    .As<Optional<object>>()
                    .ValueOrDefault! == (DateTime?)datum
                && (TestValueObject)dic[nameof(TestCommandDomainEntity.AValueObject)].As<Optional<object>>()
                    .ValueOrDefault! == valueObject
                && (Optional<TestValueObject>)dic[nameof(TestCommandDomainEntity.AnOptionalValueObject)]
                    .As<Optional<object>>()
                    .ValueOrDefault! == optionalValueObject
        )));
        domainFactory.Verify(df => df.RehydrateAggregateRoot(It.IsAny<Type>(), It.IsAny<HydrationProperties>()),
            Times.Never);
    }

    [Fact]
    public void WhenGetValueOrDefaultForValues_ThenReturnsValues()
    {
        var datum = DateTime.UtcNow;
        var valueObject = TestValueObject.Create("avalueobject").Value;
        var optionalValueObject = TestValueObject.Create("anoptionalvalueobject").Value;
        var entity = CommandEntity.FromDomainEntity(new TestCommandDomainEntity
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum,
            AnOptionalString = "anoptionalvalue",
            AnOptionalDateTime = datum,
            AnOptionalNullableString = "anoptionalvalue",
            AnOptionalNullableDateTime = datum,
            AValueObject = valueObject,
            AnOptionalValueObject = optionalValueObject
        });
        var domainFactory = new Mock<IDomainFactory>();
        domainFactory.Setup(df => df.RehydrateValueObject(typeof(TestValueObject), It.IsAny<string>()))
            .Returns(valueObject);
        domainFactory.Setup(df => df.RehydrateValueObject(typeof(Optional<TestValueObject>), It.IsAny<string>()))
            .Returns(optionalValueObject);

        entity.GetValueOrDefault<string>(nameof(TestCommandDomainEntity.Id)).Should().Be("anid");
        entity.GetValueOrDefault<string>(nameof(TestCommandDomainEntity.AStringValue)).Should().Be("avalue");
        entity.GetValueOrDefault<int>(nameof(TestCommandDomainEntity.AnIntegerValue)).Should().Be(1);
        entity.GetValueOrDefault<bool>(nameof(TestCommandDomainEntity.ABooleanValue)).Should().Be(true);
        entity.GetValueOrDefault<DateTime>(nameof(TestCommandDomainEntity.ADateTimeValue)).Should().Be(datum);
        entity.GetValueOrDefault<Optional<string>>(nameof(TestCommandDomainEntity.AnOptionalString)).Should()
            .BeSome("anoptionalvalue");
        entity.GetValueOrDefault<Optional<DateTime>>(nameof(TestCommandDomainEntity.AnOptionalDateTime)).Should()
            .BeSome(datum);
        entity.GetValueOrDefault<Optional<string?>>(nameof(TestCommandDomainEntity.AnOptionalNullableString)).Should()
            .BeSome("anoptionalvalue");
        entity.GetValueOrDefault<Optional<DateTime?>>(nameof(TestCommandDomainEntity.AnOptionalNullableDateTime))
            .Should().BeSome(datum);
        entity.GetValueOrDefault<TestValueObject>(nameof(TestCommandDomainEntity.AValueObject), domainFactory.Object)
            .Should().Be(valueObject);
        entity.GetValueOrDefault<Optional<TestValueObject>>(nameof(TestCommandDomainEntity.AnOptionalValueObject),
            domainFactory.Object).Should().Be(optionalValueObject);
    }
}