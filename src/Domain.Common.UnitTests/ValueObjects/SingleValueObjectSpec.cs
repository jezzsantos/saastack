using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.ValueObjects;

[Trait("Category", "Unit")]
public class SingleValueObjectSpec
{
    [Fact]
    public void WhenAssignInstanceToString_ThenValueAssigned()
    {
        var stringValue = new ValueObjectSpec.TestSingleStringValueObject("avalue");

        stringValue.StringValue.Should().Be("avalue");
    }

    [Fact]
    public void WhenAssignInstanceToEnumThenValueAssigned()
    {
        var enumValue = new ValueObjectSpec.TestSingleEnumValueObject(ValueObjectSpec.TestEnum.AValue1);

        enumValue.EnumValue.Should().Be(ValueObjectSpec.TestEnum.AValue1);
    }
}