using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Interfaces.UnitTests;

[Trait("Category", "Unit")]
public class QueryEntitySpec
{
    [Fact]
    public void WhenConstructed_ThenPropertiesAndMetadataIsAssigned()
    {
        var result = new QueryEntity();

        result.Id.Should().BeNone();
        result.IsDeleted.Should().BeNone();
        result.LastPersistedAtUtc.Should().BeNone();
        result.Metadata.Types.Count.Should().Be(3);
        result.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        result.Properties.Count.Should().Be(3);
        result.Properties[nameof(PersistedEntity.Id)].Should().BeNone();
        result.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        result.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
    }

    [Fact]
    public void WhenFromType_ThenPropertiesAndMetadataIsAssigned()
    {
        var datum = DateTime.UtcNow;
        var result = QueryEntity.FromType(new TestQueryDto
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
        result.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        result.Metadata.Types[nameof(TestQueryDto.AStringValue)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(TestQueryDto.AnIntegerValue)].Should().Be(typeof(int));
        result.Metadata.Types[nameof(TestQueryDto.ABooleanValue)].Should().Be(typeof(bool));
        result.Metadata.Types[nameof(TestQueryDto.ADateTimeValue)].Should().Be(typeof(DateTime));
        result.Metadata.Types[nameof(TestQueryDto.ANullableString)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(TestQueryDto.ANullableInteger)].Should().Be(typeof(int?));
        result.Metadata.Types[nameof(TestQueryDto.ANullableBoolean)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(TestQueryDto.ANullableDateTime)].Should().Be(typeof(DateTime?));
        result.Properties.Count.Should().Be(11);
        result.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        result.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        result.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        result.Properties[nameof(TestQueryDto.AStringValue)].Should().BeSome("avalue");
        result.Properties[nameof(TestQueryDto.AnIntegerValue)].Should().BeSome(1);
        result.Properties[nameof(TestQueryDto.ABooleanValue)].Should().BeSome(true);
        result.Properties[nameof(TestQueryDto.ADateTimeValue)].Should().BeSome(datum);
        result.Properties[nameof(TestQueryDto.ANullableString)].Should().BeNone();
        result.Properties[nameof(TestQueryDto.ANullableInteger)].Should().BeNone();
        result.Properties[nameof(TestQueryDto.ANullableBoolean)].Should().BeNone();
        result.Properties[nameof(TestQueryDto.ANullableDateTime)].Should().BeNone();
    }

    [Fact]
    public void
        WhenFromQueryEntityWithMatchingPropertiesAndOtherQueryEntityAsMetadata_ThenReturnsEntityWithOnlyMatchingProperties()
    {
        var datum = DateTime.UtcNow;
        var otherInstance = QueryEntity.FromType(new TestQueryDto
        {
            Id = "anotherid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum
        }).Metadata;

        var result = QueryEntity.FromQueryEntity(new HydrationProperties
        {
            { nameof(IHasIdentity.Id), "anid" },
            { nameof(TestDto.AStringValue), "avalue" },
            { nameof(TestDto.ANullableString), (string?)null }
        }, otherInstance);

        result.Id.Should().BeSome("anid");
        result.IsDeleted.Should().BeNone();
        result.LastPersistedAtUtc.Should().BeNone();
        result.Metadata.Types.Count.Should().Be(5);
        result.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        result.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        result.Metadata.Types[nameof(TestDto.AStringValue)].Should().Be(typeof(string));
        result.Metadata.Types[nameof(TestDto.ANullableString)].Should().Be(typeof(string));
        result.Properties.Count.Should().Be(5);
        result.Properties[nameof(PersistedEntity.Id)].Should().BeSome("anid");
        result.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        result.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        result.Properties[nameof(TestQueryDto.AStringValue)].Should().BeSome("avalue");
        result.Properties[nameof(TestQueryDto.ANullableString)].Should().BeNone();
    }

    [Fact]
    public void WhenAddWithNoneValue_ThenAddsNullValueProperty()
    {
        var entity = new QueryEntity();

        entity.Add("aname", Optional<string>.None);

        entity.Id.Should().BeNone();
        entity.IsDeleted.Should().BeNone();
        entity.LastPersistedAtUtc.Should().BeNone();
        entity.Metadata.Types.Count.Should().Be(4);
        entity.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        entity.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        entity.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        entity.Metadata.Types["aname"].Should().Be(typeof(string));
        entity.Properties.Count.Should().Be(4);
        entity.Properties[nameof(PersistedEntity.Id)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        entity.Properties["aname"].Should().BeNone();
    }

    [Fact]
    public void WhenAddWithPrimitiveValue_ThenAddsProperty()
    {
        var entity = new QueryEntity();

        entity.Add("aname", "avalue");

        entity.Id.Should().BeNone();
        entity.IsDeleted.Should().BeNone();
        entity.LastPersistedAtUtc.Should().BeNone();
        entity.Metadata.Types.Count.Should().Be(4);
        entity.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        entity.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        entity.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        entity.Metadata.Types["aname"].Should().Be(typeof(string));
        entity.Properties.Count.Should().Be(4);
        entity.Properties[nameof(PersistedEntity.Id)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        entity.Properties["aname"].Should().BeSome("avalue");
    }

    [Fact]
    public void WhenAddWithValueObjectValue_ThenUpdatesProperty()
    {
        var entity = new QueryEntity();

        entity.Add("aname", TestValueObject.Create("avalue").Value);

        entity.Id.Should().BeNone();
        entity.IsDeleted.Should().BeNone();
        entity.LastPersistedAtUtc.Should().BeNone();
        entity.Metadata.Types.Count.Should().Be(4);
        entity.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        entity.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        entity.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        entity.Metadata.Types["aname"].Should().Be(typeof(TestValueObject));
        entity.Properties.Count.Should().Be(4);
        entity.Properties[nameof(PersistedEntity.Id)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        entity.Properties["aname"].Should().BeSome("avalue");
    }

    [Fact]
    public void WhenAddAndValueAlreadyExists_ThenUpdatesProperty()
    {
        var entity = new QueryEntity();

        entity.Add("aname", "avalue1");
        entity.Add("aname", "avalue2");

        entity.Id.Should().BeNone();
        entity.IsDeleted.Should().BeNone();
        entity.LastPersistedAtUtc.Should().BeNone();
        entity.Metadata.Types.Count.Should().Be(4);
        entity.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        entity.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        entity.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        entity.Metadata.Types["aname"].Should().Be(typeof(string));
        entity.Properties.Count.Should().Be(4);
        entity.Properties[nameof(PersistedEntity.Id)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        entity.Properties["aname"].Should().BeSome("avalue2");
    }

    [Fact]
    public void WhenAddWithNewDataTypeAndValueAlreadyExists_ThenUpdatesProperty()
    {
        var entity = new QueryEntity();

        entity.Add("aname", "avalue1");
        entity.Add("aname", 1);

        entity.Id.Should().BeNone();
        entity.IsDeleted.Should().BeNone();
        entity.LastPersistedAtUtc.Should().BeNone();
        entity.Metadata.Types.Count.Should().Be(4);
        entity.Metadata.Types[nameof(PersistedEntity.Id)].Should().Be(typeof(string));
        entity.Metadata.Types[nameof(PersistedEntity.IsDeleted)].Should().Be(typeof(bool?));
        entity.Metadata.Types[nameof(PersistedEntity.LastPersistedAtUtc)].Should().Be(typeof(DateTime?));
        entity.Metadata.Types["aname"].Should().Be(typeof(int));
        entity.Properties.Count.Should().Be(4);
        entity.Properties[nameof(PersistedEntity.Id)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.IsDeleted)].Should().BeNone();
        entity.Properties[nameof(PersistedEntity.LastPersistedAtUtc)].Should().BeNone();
        entity.Properties["aname"].Should().BeSome(1);
    }

    [Fact]
    public void WhenToDomainEntityWithDomainFactory_ThenReturnsInstance()
    {
        var valueObject = TestValueObject.Create("avalueobject").Value;
        var datum = DateTime.UtcNow;
        var entity = QueryEntity.FromType(new TestQueryDomainEntity
        {
            Id = "anid".ToId(),
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum,
            AValueObject = valueObject
        });
        var domainFactory = new Mock<IDomainFactory>();
        domainFactory.Setup(df => df.RehydrateValueObject(typeof(ISingleValueObject<string>), It.IsAny<string>()))
            .Returns("anid".ToId());
        domainFactory.Setup(df => df.RehydrateValueObject(typeof(TestValueObject), It.IsAny<string>()))
            .Returns(valueObject);

        var result = entity.ToDomainEntity<TestQueryDomainEntity>(domainFactory.Object);

        result.Id.Value.Should().Be("anid");
        result.AStringValue.Should().Be("avalue");
        result.AnIntegerValue.Should().Be(1);
        result.ABooleanValue.Should().Be(true);
        result.ADateTimeValue.Should().Be(datum);
        result.ANullableString.Should().BeNull();
        result.ANullableInteger.Should().BeNull();
        result.ANullableBoolean.Should().BeNull();
        result.ANullableDateTime.Should().BeNull();
        result.AValueObject.Should().Be(valueObject);
        domainFactory.Verify(df => df.RehydrateValueObject(typeof(ISingleValueObject<string>), "anid"));
        domainFactory.Verify(df => df.RehydrateValueObject(typeof(TestValueObject), "avalueobject"));
    }

    [Fact]
    public void WhenToDto_ThenReturnsInstance()
    {
        var datum = DateTime.UtcNow;
        var entity = QueryEntity.FromType(new TestQueryDto
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum
        });

        var result = entity.ToDto<TestQueryDto>();

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
}