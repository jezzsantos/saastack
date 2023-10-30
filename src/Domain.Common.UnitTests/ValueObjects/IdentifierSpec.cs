using AutoMapper;
using Domain.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.ValueObjects;

[Trait("Category", "Unit")]
public class IdentifierSpec
{
    [Fact]
    public void WhenAutoMapperMapsIdentifier_ThenMapsToStringValue()
    {
        var @object = new TestObject
        {
            StringValue = Identifier.Create("avalue")
        };

        var config = new MapperConfiguration(cfg => cfg.CreateMap<TestObject, TestDto>());
        var mapper = config.CreateMapper();

        var result = mapper.Map<TestDto>(@object);

        result.StringValue.Should().Be("avalue");
    }
}

public class TestObject
{
    public Identifier? StringValue { get; set; }
}

public class TestDto
{
    public string? StringValue { get; set; }
}