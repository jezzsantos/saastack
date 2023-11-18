using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class OptionalSpec
{
    [Fact]
    public void WhenTryGetContainedTypeAndNotOptionalType_ThenReturnsFalse()
    {
        var result = Optional.TryGetContainedType(typeof(string), out var containedType);

        result.Should().BeFalse();
        containedType.Should().BeNull();
    }

    [Fact]
    public void WhenTryGetContainedTypeAndOptionalType_ThenReturnsTrue()
    {
        var result = Optional.TryGetContainedType(typeof(Optional<string>), out var containedType);

        result.Should().BeTrue();
        containedType.Should().Be(typeof(string));
    }

    [Fact]
    public void WhenIsOptionalAndValueIsNull_ThenReturnsFalse()
    {
        var result = ((string?)null).IsOptional(out var contained);

        result.Should().BeFalse();
        contained.Should().BeNull();
    }

    [Fact]
    public void WhenIsOptionalAndValueIsNotOptional_ThenReturnsFalse()
    {
        var result = string.Empty.IsOptional(out var contained);

        result.Should().BeFalse();
        contained.Should().BeNull();
    }

    [Fact]
    public void WhenIsOptionalAndValueIsOptionalNone_ThenReturnsTrue()
    {
        var result = Optional<object>.None.IsOptional(out var contained);

        result.Should().BeTrue();
        contained.Should().BeNull();
    }

    [Fact]
    public void WhenIsOptionalAndValueIsOptionalString_ThenReturnsTrue()
    {
        var result = new Optional<string>("avalue")
            .IsOptional(out var contained);

        result.Should().BeTrue();
        contained.Should().Be("avalue");
    }

    [Fact]
    public void WhenNone_ThenReturnsNone()
    {
        var result = Optional.None<string>();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenSomeWithNull_ThenThrows()
    {
        FluentActions.Invoking(() => Optional.Some<string>(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WhenSomeWithValue_ThenReturnsOptional()
    {
        var result = Optional.Some<string>("avalue");

        result.Should().BeSome("avalue");
    }

    [Fact]
    public void WhenSomeWithOptionalOfSameType_ThenReturnsSome()
    {
        var optional = (string)new Optional<string>("avalue");
        var result = Optional.Some(optional);

        result.Should().BeSome("avalue");
        result.Should().Be(optional);
    }

    [Fact]
    public void WhenSomeWithWithOptionalOfDifferentType_ThenReturnsSome()
    {
        var optional = new Optional<string>("avalue");
        var result = Optional.Some<object>(optional);

        result.Should().BeSome("avalue");
    }

    [Fact]
    public void WhenToOptionalWithNull_ThenReturnsNone()
    {
        var result = ((string?)null).ToOptional();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenToOptionalWithOptionalNone_ThenReturnsNone()
    {
        var optional = Optional<string>.None;
        var result = optional.ToOptional();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenToOptionalWithOptionalOfSameType_ThenReturnsSome()
    {
        var optional = (string)new Optional<string>("avalue");
        var result = optional.ToOptional();

        result.Should().BeSome("avalue");
        result.Should().Be(optional);
    }

    [Fact]
    public void WhenToOptionalWithWithOptionalOfDifferentType_ThenReturnsSome()
    {
        var optional = new Optional<string>("avalue");
        var result = optional.ToOptional<object>();

        result.Should().BeSome("avalue");
    }

    [Fact]
    public void WhenToOptionalWithValue_ThenReturnsSome()
    {
        var result = "avalue".ToOptional();

        result.Should().BeSome("avalue");
    }
}

[Trait("Category", "Unit")]
public class OptionalOfTSpec
{
    [Fact]
    public void WhenConstructedWithoutAnyValue_ThenHasNoValue()
    {
        var result = new Optional<TestClass>();

        result.HasValue.Should().BeFalse();
        result.TryGet(out _).Should().BeFalse();
        result.ToString().Should().Be(Optional<TestClass>.NoValueStringValue);
    }

    [Fact]
    public void WhenConstructedWithNullInstance_ThenHasNoValue()
    {
        var result = new Optional<TestClass>((TestClass)null!);

        result.HasValue.Should().BeFalse();
        result.TryGet(out _).Should().BeFalse();
        result.ToString().Should().Be(Optional<TestClass>.NoValueStringValue);
    }

    [Fact]
    public void WhenConstructedWithAnyValue_ThenHasValue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var result = new Optional<TestClass>(instance);

        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(instance);
        result.ToString().Should().Be(typeof(TestClass).FullName);
    }

    [Fact]
    public void WhenConstructedWithAnyOptional_ThenHasValue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>(instance);
        var result = new Optional<TestClass>(optional);

        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(instance);
        result.ToString().Should().Be(typeof(TestClass).FullName);
    }

    [Fact]
    public void WhenConstructedWithAnyOptionalOfOptional_ThenHasValue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optionalInner = new Optional<TestClass>(instance);
        var optionalOuter = new Optional<Optional<TestClass>>(optionalInner);
        var result = new Optional<TestClass>(optionalOuter);

        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(instance);
        result.ToString().Should().Be(typeof(TestClass).FullName);
    }

    [Fact]
    public void WhenGetValueAndNullInstance_ThenThrows()
    {
        var optional = new Optional<TestClass>((TestClass)null!);

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
        var optional = new Optional<TestClass>((TestClass)null!);

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

        var result = optional1 == optional2;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsOperatorWithNoneAndWithNone_ThenReturnsTrue()
    {
        var optional1 = Optional<TestClass>.None;
        var optional2 = Optional<TestClass>.None;

        var result = optional1 == optional2;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsOperatorWithEmptyOptionals_ThenReturnsTrue()
    {
        var optional1 = new Optional<TestClass>();
        var optional2 = new Optional<TestClass>();

        var result = optional1 == optional2;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsOperatorWithSameOptionals_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional1 = new Optional<TestClass>(instance);
        var optional2 = new Optional<TestClass>(instance);

        var result = optional1 == optional2;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithSameOptionals_ThenReturnsFalse()
    {
        var optional1 = new Optional<TestClass>();
        var optional2 = new Optional<TestClass>();

        var result = optional1 != optional2;

        result.Should().BeFalse();
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

        var result = instance == null!;

        result.Should().BeFalse();
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

        var result = instance != optional;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithNull_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };

        var result = instance != null!;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorOptionalOfInstance_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>(instance);

        var result = optional != instance;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithEmptyOptionalOfSameType_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>();

        var result = optional.Equals(instance);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithNull_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue" };

        var result = instance.Equals(null!);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOptionalOfInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>(instance);

        var result = optional.Equals(instance);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenObjectEqualsBetweenNullOptionalAndInstance_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>((TestClass)null!);

        // ReSharper disable once SuspiciousTypeConversion.Global
        var result = optional.Equals((object?)instance);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenObjectEqualsBetweenEmptyOptionalAndInstanceOfSameType_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>();

        // ReSharper disable once SuspiciousTypeConversion.Global
        var result = optional.Equals((object?)instance);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenObjectEqualsBetweenOptionOfInstanceAndInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>(instance);

        // ReSharper disable once SuspiciousTypeConversion.Global
        var result = optional.Equals((object?)instance);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenObjectEqualsBetweenOptionalOfInstanceAndOptionalOfInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>(instance);

        var result = optional.Equals((object?)optional);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithEmptyOptionalAndNullInstance_ThenReturnsFalse()
    {
        var optional = new Optional<TestClass>();

        var result = optional != (Optional<TestClass>)null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithEmptyOptionalAndInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>();

        var result = optional != instance;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithOtherOptionalAndOtherInstance_ThenReturnsTrue()
    {
        var instance1 = new TestClass { AProperty = "avalue1" };
        var instance2 = new TestClass { AProperty = "avalue2" };
        var optional = new Optional<TestClass>(instance1);

        var result = optional != instance2;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithOptionalOfInstanceAndInstance_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue1" };
        var optional = new Optional<TestClass>(instance);

        var result = optional != instance;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithNullInstanceAndEmptyOptional_ThenReturnsFalse()
    {
        var optional = new Optional<TestClass>();

        var result = (Optional<TestClass>)null! != optional;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithInstanceAndEmptyOptional_ThenReturnsTrue()
    {
        var instance = new TestClass { AProperty = "avalue" };
        var optional = new Optional<TestClass>();

        var result = instance != optional;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithInstanceAndOptionalOfOtherInstance_ThenReturnsTrue()
    {
        var instance1 = new TestClass { AProperty = "avalue1" };
        var instance2 = new TestClass { AProperty = "avalue2" };
        var optional = new Optional<TestClass>(instance1);

        var result = instance2 != optional;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithInstanceAndOptionalOfInstance_ThenReturnsFalse()
    {
        var instance = new TestClass { AProperty = "avalue1" };
        var optional = new Optional<TestClass>(instance);

        var result = instance != optional;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenSomeWithNull_ThenReturnsNone()
    {
        var result = Optional<string>.Some(null!);

        result.Should().BeNone();
    }

    [Fact]
    public void WhenSomeWithOptional_ThenReturnsOptional()
    {
        var optional = new Optional<string>("avalue");

        var result = Optional<string>.Some(optional);

        result.Should().Be(optional);
        result.Should().BeSome("avalue");
    }

    [Fact]
    public void WhenSomeWithValue_ThenReturnsOptional()
    {
        var result = Optional<string>.Some("avalue");

        result.Should().BeSome("avalue");
    }
}

public class TestClass
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public required string AProperty { get; set; }
}