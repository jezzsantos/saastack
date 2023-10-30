using FluentAssertions;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class OptionalSpec
{
    [Fact]
    public void WhenConstructedWithoutAnyValue_ThenHasNoValue()
    {
        var result = new Optional<TestClass>();

        result.HasValue.Should().BeFalse();
        result.TryGet(out _)
            .Should().BeFalse();
        result.ToString()
            .Should().Be(Optional<TestClass>.NoValueStringValue);
    }

    [Fact]
    public void WhenConstructedWithNullInstance_ThenHasNoValue()
    {
        var result = new Optional<TestClass>(null!);

        result.HasValue.Should().BeFalse();
        result.TryGet(out _)
            .Should().BeFalse();
        result.ToString()
            .Should().Be(Optional<TestClass>.NoValueStringValue);
    }

    [Fact]
    public void WhenConstructedWithAnyValue_ThenHasValue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var result = new Optional<TestClass>(instance);

        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(instance);
        result.ToString()
            .Should().Be(typeof(TestClass).FullName);
    }

    [Fact]
    public void WhenGetValueAndNullInstance_ThenThrows()
    {
        var optional = new Optional<TestClass>(null!);

        optional.Invoking(x => x.Value)
            .Should().Throw<InvalidOperationException>()
            .WithMessage(Resources.Optional_NullValue);
    }

    [Fact]
    public void WhenGetValue_ThenReturnsValue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>(instance);

        var result = optional.Value;

        result.Should().Be(instance);
    }

    [Fact]
    public void WhenGetValueOrDefaultAndNullInstance_ThenReturnsNull()
    {
        var optional = new Optional<TestClass>(null!);

        var result = optional.ValueOrDefault;

        result.Should().BeNull();
    }

    [Fact]
    public void WhenGetValueOrDefault_ThenReturnsValue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>(instance);

        var result = optional.ValueOrDefault;

        result.Should().Be(instance);
    }
    
    [Fact]
    public void WhenEqualsOperatorWithEmptyOptionalAndWithNone_ThenReturnsTrue()
    {
        var optional1 = new Optional<TestClass>();
        var optional2 = Optional<TestClass>.None;

        (optional1 == optional2).Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsOperatorWithNoneAndWithNone_ThenReturnsTrue()
    {
        var optional1 = Optional<TestClass>.None;
        var optional2 = Optional<TestClass>.None;

        (optional1 == optional2).Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsOperatorWithEmptyOptionals_ThenReturnsTrue()
    {
        var optional1 = new Optional<TestClass>();
        var optional2 = new Optional<TestClass>();

        (optional1 == optional2).Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsOperatorWithSameOptionals_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional1 = new Optional<TestClass>(instance);
        var optional2 = new Optional<TestClass>(instance);

        (optional1 == optional2).Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithSameOptionals_ThenReturnsFalse()
    {
        var optional1 = new Optional<TestClass>();
        var optional2 = new Optional<TestClass>();

        (optional1 != optional2).Should().BeFalse();
    }
    

    [Fact]
    public void WhenEqualsOperatorWithEmptyOptionalOfSameType_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>();

        (instance == optional).Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorWithNull_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue" };

        (instance == null!).Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorOptionalOfInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>(instance);

        (optional == instance).Should().BeTrue();
    }


    [Fact]
    public void WhenNotEqualsOperatorWithEmptyOptionalOfSameType_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>();

        (instance != optional).Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithNull_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };

        (instance != null!).Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorOptionalOfInstance_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>(instance);

        (optional != instance).Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithEmptyOptionalOfSameType_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>();

        optional.Equals(instance)
            .Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithNull_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue" };

        instance.Equals(null!)
            .Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOptionalOfInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>(instance);

        optional.Equals(instance)
            .Should().BeTrue();
    }

    [Fact]
    public void WhenObjectEqualsBetweenNullOptionalAndInstance_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>(null!);

        // ReSharper disable once SuspiciousTypeConversion.Global
        optional.Equals((object?)instance)
            .Should().BeFalse();
    }

    [Fact]
    public void WhenObjectEqualsBetweenEmptyOptionalAndInstanceOfSameType_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>();

        // ReSharper disable once SuspiciousTypeConversion.Global
        optional.Equals((object?)instance)
            .Should().BeFalse();
    }

    [Fact]
    public void WhenObjectEqualsBetweenOptionOfInstanceAndInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>(instance);

        // ReSharper disable once SuspiciousTypeConversion.Global
        optional.Equals((object?)instance)
            .Should().BeTrue();
    }

    [Fact]
    public void WhenObjectEqualsBetweenOptionalOfInstanceAndOptionalOfInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>(instance);

        optional.Equals((object?)optional)
            .Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithEmptyOptionalAndNullInstance_ThenReturnsTrue()
    {
        var optional = new Optional<TestClass>();

        (optional != null!)
            .Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithEmptyOptionalAndInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>();

        (optional != instance)
            .Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithOtherOptionalAndOtherInstance_ThenReturnsTrue()
    {
        var instance1 = new TestClass { AProperty = "avalue1" };
        var instance2 = new TestClass { AProperty = "avalue2" };
        var optional = new Optional<TestClass>(instance1);

        (optional != instance2)
            .Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithOptionalOfInstanceAndInstance_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue1" };
        var optional = new Optional<TestClass>(instance);

        (optional != instance)
            .Should().BeFalse();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithNullInstanceAndEmptyOptional_ThenReturnsTrue()
    {
        var optional = new Optional<TestClass>();

        (null! != optional)
            .Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithInstanceAndEmptyOptional_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>();

        (instance != optional)
            .Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithInstanceAndOptionalOfOtherInstance_ThenReturnsTrue()
    {
        var instance1 = new TestClass { AProperty = "avalue1" };
        var instance2 = new TestClass { AProperty = "avalue2" };
        var optional = new Optional<TestClass>(instance1);

        (instance2 != optional)
            .Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithInstanceAndOptionalOfInstance_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue1" };
        var optional = new Optional<TestClass>(instance);

        (instance != optional)
            .Should().BeFalse();
    }
}

public class TestClass
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public required string AProperty { get; set; }
}