using Domain.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.ValueObjects;

[Trait("Category", "Unit")]
public class ValueObjectExtensionsSpec
{
    [Fact]
    public void WhenHasValueAndValueIsNull_ThenReturnsFalse()
    {
        var result = ((ValueObjectSpec.TestSingleStringValueObject)null!).HasValue();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasValueAndValueIsNotNull_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue").HasValue();

        result.Should().BeTrue();
    }
}