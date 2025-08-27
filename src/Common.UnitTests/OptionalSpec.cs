using Common.Extensions;
using FluentAssertions;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class OptionalSpec
{
    [Fact]
    public void WhenTryGetContainedTypeAndNotOptionalType_ThenReturnsFalse()
    {
        var result = Optional.TryGetOptionalType(typeof(string), out var containedType);

        result.Should().BeFalse();
        containedType.Should().BeNull();
    }

    [Fact]
    public void WhenTryGetContainedTypeAndOptionalType_ThenReturnsTrue()
    {
        var result = Optional.TryGetOptionalType(typeof(Optional<string>), out var containedType);

        result.Should().BeTrue();
        containedType.Should().Be(typeof(string));
    }

    [Fact]
    public void WhenIsOptionalAndValueIsNull_ThenReturnsFalse()
    {
        var result = ((string?)null)
            .IsOptional(out var descriptor);

        result.Should().BeFalse();
        descriptor.Should().BeNull();
    }

    [Fact]
    public void WhenIsOptionalAndValueIsNotOptional_ThenReturnsFalse()
    {
        var result = string.Empty
            .IsOptional(out var descriptor);

        result.Should().BeFalse();
        descriptor.Should().BeNull();
    }

    [Fact]
    public void WhenIsOptionalAndValueIsOptionalNone_ThenReturnsInfo()
    {
        var result = Optional<object>.None
            .IsOptional(out var descriptor);

        result.Should().BeTrue();
        descriptor.Should().NotBeNull();
        descriptor!.ContainedType.Should().Be(typeof(object));
        descriptor.ContainedValue.Should().BeNull();
        descriptor.IsNone.Should().BeTrue();
    }

    [Fact]
    public void WhenIsOptionalAndValueIsOptionalString_ThenReturnsInfo()
    {
        var result = new Optional<string>("avalue")
            .IsOptional(out var descriptor);

        result.Should().BeTrue();
        descriptor.Should().NotBeNull();
        descriptor!.ContainedType.Should().Be(typeof(string));
        descriptor.ContainedValue.Should().Be("avalue");
        descriptor.IsNone.Should().BeFalse();
    }

    [Fact]
    public void WhenNone_ThenReturnsNone()
    {
        var result = Optional.None<string>();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenSomeWithNull_ThenReturnsNone()
    {
        var result = Optional.Some<string>(null);

        result.Should().BeNone();
        result.Should().BeOfType<Optional<string>>();
    }

    [Fact]
    public void WhenSomeWithAValue_ThenReturnsOptional()
    {
        var result = Optional.Some<string>("avalue");

        result.Should().BeSome("avalue");
        result.Should().BeOfType<Optional<string>>();
    }

    [Fact]
    public void WhenSomeWithOptional_ThenThrows()
    {
        var optional = new Optional<string>("avalue");

        FluentActions.Invoking(() => Optional.Some(optional))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.Optional_WrappingOptional);
    }

    [Fact]
    public void WhenSomeWithWithOptionalNone_ThenThrows()
    {
        var optional = new Optional<string>();

        FluentActions.Invoking(() => Optional.Some(optional))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.Optional_WrappingOptional);
    }

    [Fact]
    public void WhenToOptionalWithNull_ThenReturnsNone()
    {
        var result = ((string?)null).ToOptional();

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
    public void WhenToOptionalWithWithOptional_ThenThrows()
    {
        var optional = new Optional<string>("avalue");

        optional.Invoking(x => x.ToOptional<object>())
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.Optional_WrappingOptional);
    }

    [Fact]
    public void WhenToOptionalWithValue_ThenReturnsSome()
    {
        var result = "avalue".ToOptional();

        result.Should().BeSome("avalue");
    }

    [Fact]
    public void WhenToOptionalAndInputIsNullWithReferenceTypeAndNoConverter_ThenReturnsNone()
    {
        var value = (string?)null;

        var result = value.ToOptional();

        result.Should().BeNone();
        result.Should().Be(Optional<string>.None);
    }

    [Fact]
    public void WhenToOptionalAndInputHasValueWithReferenceTypeAndNoConverter_ThenReturnsSome()
    {
        var result = "avalue".ToOptional();

        result.Should().BeSome("avalue");
        result.Should().BeOfType<Optional<string>>();
    }

    [Fact]
    public void WhenToOptionalAndInputIsNullWithReferenceTypeAndConverter_ThenReturnsNone()
    {
        var value = (string?)null;

        var result = value.ToOptional(_ => "anewvalue");

        result.Should().BeNone();
        result.Should().BeOfType<Optional<string>>();
    }

    [Fact]
    public void WhenToOptionalAndInputHasValueWithReferenceTypeAndConverter_ThenReturnsConvertedSome()
    {
        var result = "avalue".ToOptional(_ => "anewvalue");

        result.Should().BeSome("anewvalue");
        result.Should().BeOfType<Optional<string>>();
    }

    [Fact]
    public void WhenToOptionalAndInputIsNullWithNullableValueTypeAndNoConverter_ThenReturnsNone()
    {
        var datum = (DateTime?)null;

        var result = datum.ToOptional();

        result.Should().BeNone();
        result.Should().BeOfType<Optional<DateTime>>();
    }

    [Fact]
    public void WhenToOptionalAndInputHasValueWithNullableValueTypeAndNoConverter_ThenReturnsNone()
    {
        var datum = (DateTime?)DateTime.UtcNow;

        var result = datum.ToOptional();

        result.Should().BeSome(datum.Value);
        result.Should().BeOfType<Optional<DateTime>>();
    }

    [Fact]
    public void WhenToOptionalAndInputHasValueWithValueTypeAndNoConverter_ThenReturnsSome()
    {
        var datum = DateTime.UtcNow;

        var result = datum.ToOptional();

        result.Should().BeSome(datum);
        result.Should().BeOfType<Optional<DateTime>>();
    }

    [Fact]
    public void WhenToOptionalAndInputHasValueWithNullableValueTypeAndNoConverter_ThenReturnsSome()
    {
        var datum = (DateTime?)DateTime.UtcNow;

        var result = datum.ToOptional();

        result.Should().BeSome(datum.Value);
        result.Should().BeOfType<Optional<DateTime>>();
    }

    [Fact]
    public void WhenToOptionalAndInputHasValueWithNullableValueTypeAndConverter_ThenReturnsConvertedSome()
    {
        var datum = (DateTime?)DateTime.UtcNow.SubtractSeconds(1);
        var newDate = DateTime.UtcNow.AddSeconds(1);

        var result = datum.ToOptional<DateTime?, DateTime>(_ => newDate);

        result.Should().BeSome(newDate);
        result.Should().BeOfType<Optional<DateTime>>();
    }

    [Fact]
    public void WhenToOptionalAndInputHasValueWithValueTypeAndConverter_ThenReturnsConvertedSome()
    {
        var datum = DateTime.UtcNow.SubtractSeconds(1);
        var newDate = DateTime.UtcNow.AddSeconds(1);

        var result = datum.ToOptional<DateTime, DateTime>(_ => newDate);

        result.Should().BeSome(newDate);
        result.Should().BeOfType<Optional<DateTime>>();
    }

    [Fact]
    public void WhenToNullableAndInputIsNoneReferenceTypeAndNoConverter_ThenReturnsNull()
    {
        var optional = Optional<string>.None;

        var result = optional.ToNullable();

        result.Should().BeNull();
    }

    [Fact]
    public void WhenToNullableAndInputIsNoneNullableReferenceTypeAndNoConverter_ThenReturnsNull()
    {
        var optional = Optional<string?>.None;

        var result = optional.ToNullable();

        result.Should().BeNull();
    }

    [Fact]
    public void WhenToNullableAndInputIsSomeReferenceTypeAndNoConverter_ThenReturnsSome()
    {
        var optional = new Optional<string>("avalue");

        var result = optional.ToNullable();

        result.Should().Be("avalue");
    }

    [Fact]
    public void WhenToNullableAndInputIsSomeReferenceTypeAndConverterForReferenceType_ThenReturnsSome()
    {
        var optional = new Optional<TestClass>(new TestClass
        {
            AStringProperty = "avalue",
            ADateTimeProperty = DateTime.UtcNow
        });

        var result = optional.ToNullable(x => x.AStringProperty);

        result.Should().Be("avalue");
    }

    [Fact]
    public void WhenToNullableAndInputIsSomeReferenceTypeAndConverterForValueType_ThenReturnsSome()
    {
        var datum = DateTime.UtcNow;
        var optional = new Optional<TestClass>(new TestClass
        {
            AStringProperty = "avalue",
            ADateTimeProperty = datum
        });

        var result = optional.ToNullable<TestClass, DateTime>(x => x.ADateTimeProperty);

        result.Should().Be(datum);
    }

    [Fact]
    public void WhenToNullableAndInputIsNoneValueTypeAndNoConverter_ThenReturnsNull()
    {
        var optional = Optional<DateTime>.None;

        var result = optional.ToNullable();

        result.Should().BeNull();
    }

    [Fact]
    public void WhenToNullableAndInputIsNoneNullableValueTypeAndNoConverter_ThenReturnsNull()
    {
        var optional = Optional<DateTime?>.None;

        var result = optional.ToNullable();

        result.Should().BeNull();
    }

    [Fact]
    public void WhenToNullableAndInputIsSomeValueTypeAndNoConverter_ThenReturnsSome()
    {
        var datum = DateTime.UtcNow;
        var optional = new Optional<DateTime>(datum);

        var result = optional.ToNullable();

        result.Should().Be(datum);
    }

    [Fact]
    public void WhenToNullableAndInputIsSomeValueTypeAndConverterForReferenceType_ThenReturnsSome()
    {
        var optional = new Optional<TestStruct>(new TestStruct
        {
            AStringProperty = "avalue",
            ADateTimeProperty = DateTime.UtcNow
        });

        var result = optional.ToNullable(x => x.AStringProperty);

        result.Should().Be("avalue");
    }

    [Fact]
    public void WhenToNullableAndInputIsSomeValueTypeAndConverterForValueType_ThenReturnsSome()
    {
        var datum = DateTime.UtcNow;
        var optional = new Optional<TestStruct>(new TestStruct
        {
            AStringProperty = "avalue",
            ADateTimeProperty = datum
        });

        var result = optional.ToNullable<TestStruct, DateTime>(x => x.ADateTimeProperty);

        result.Should().Be(datum);
    }
}

[Trait("Category", "Unit")]
public class OptionalOfTSpec
{
    [Fact]
    public void WhenSomeWithNull_ThenReturnsNone()
    {
        var result = Optional<string>.Some(null!);

        result.Should().BeNone();
    }

    [Fact]
    public void WhenSomeWithOptional_ThenThrows()
    {
        var optional = new Optional<string>("avalue");

        FluentActions.Invoking(() => Optional<Optional<string>>.Some(optional))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.Optional_WrappingOptional);
    }

    [Fact]
    public void WhenSomeWithValue_ThenReturnsOptional()
    {
        var result = Optional<string>.Some("avalue");

        result.Should().BeSome("avalue");
    }

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
        var instance = new TestClass
        {
            AStringProperty = "avalue",
            ADateTimeProperty = DateTime.UtcNow
        };
        var result = new Optional<TestClass>(instance);

        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(instance);
        result.ToString().Should().Be(typeof(TestClass).FullName);
    }

    [Fact]
    public void WhenConstructedWithAnyOptional_ThenHasValue()
    {
        var instance = new TestClass
        {
            AStringProperty = "avalue",
            ADateTimeProperty = DateTime.UtcNow
        };
        var optional = new Optional<TestClass>(instance);
        var result = new Optional<TestClass>(optional);

        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(instance);
        result.ToString().Should().Be(typeof(TestClass).FullName);
    }

    [Fact]
    public void WhenConstructedWithAnyOptionalOfOptional_ThenHasValue()
    {
        var instance = new TestClass
        {
            AStringProperty = "avalue",
            ADateTimeProperty = DateTime.UtcNow
        };
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
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
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
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
        var optional = new Optional<TestClass>(instance);

        var result = optional.ValueOrDefault;

        result.Should().Be(instance);
    }

    [Fact]
    public void WhenEqualsOperatorWithEmptyAndNone_ThenReturnsTrue()
    {
        var optional1 = new Optional<TestClass>();
        var optional2 = Optional<TestClass>.None;

        var result = optional1 == optional2;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsOperatorWithNoneAndEmpty_ThenReturnsTrue()
    {
        var optional1 = Optional<TestClass>.None;
        var optional2 = new Optional<TestClass>();

        var result = optional1 == optional2;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsOperatorWithNoneAndNone_ThenReturnsTrue()
    {
        var optional1 = Optional<TestClass>.None;
        var optional2 = Optional<TestClass>.None;

        var result = optional1 == optional2;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsOperatorWithEmptyAndEmpty_ThenReturnsTrue()
    {
        var optional1 = new Optional<TestClass>();
        var optional2 = new Optional<TestClass>();

        var result = optional1 == optional2;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsOperatorWithEmptyAndSome_ThenReturnsFalse()
    {
        var optional1 = new Optional<TestClass>();
        var optional2 = new Optional<TestClass>(new TestClass
        {
            AStringProperty = "avalue",
            ADateTimeProperty = DateTime.UtcNow
        });

        var result = optional1 == optional2;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorWithSomeAndEmpty_ThenReturnsFalse()
    {
        var optional1 = new Optional<TestClass>(new TestClass
        {
            AStringProperty = "avalue",
            ADateTimeProperty = DateTime.UtcNow
        });
        var optional2 = new Optional<TestClass>();

        var result = optional1 == optional2;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorWithNoneAndSome_ThenReturnsFalse()
    {
        var optional1 = Optional<TestClass>.None;
        var optional2 = new Optional<TestClass>(new TestClass
        {
            AStringProperty = "avalue",
            ADateTimeProperty = DateTime.UtcNow
        });

        var result = optional1 == optional2;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorWithSomeAndNone_ThenReturnsFalse()
    {
        var optional1 = new Optional<TestClass>(new TestClass
        {
            AStringProperty = "avalue",
            ADateTimeProperty = DateTime.UtcNow
        });
        var optional2 = Optional<TestClass>.None;

        var result = optional1 == optional2;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorWithSameInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
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
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
        var optional = new Optional<TestClass>();

        (instance == optional).Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorWithNull_ThenReturnsFalse()
    {
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };

        var result = instance == null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorOptionalOfInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
        var optional = new Optional<TestClass>(instance);

        (optional == instance).Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithEmptyOptionalOfSameType_ThenReturnsTrue()
    {
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
        var optional = new Optional<TestClass>();

        var result = instance != optional;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithNull_ThenReturnsTrue()
    {
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };

        var result = instance != null!;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorOptionalOfInstance_ThenReturnsFalse()
    {
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
        var optional = new Optional<TestClass>(instance);

        var result = optional != instance;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithEmptyOptionalOfSameType_ThenReturnsFalse()
    {
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
        var optional = new Optional<TestClass>();

        var result = optional.Equals(instance);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithNull_ThenReturnsFalse()
    {
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };

        var result = instance.Equals(null!);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOptionalOfInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
        var optional = new Optional<TestClass>(instance);

        var result = optional.Equals(instance);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenObjectEqualsBetweenNullOptionalAndInstance_ThenReturnsFalse()
    {
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
        var optional = new Optional<TestClass>((TestClass)null!);

        // ReSharper disable once SuspiciousTypeConversion.Global
        var result = optional.Equals((object?)instance);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenObjectEqualsBetweenEmptyOptionalAndInstanceOfSameType_ThenReturnsFalse()
    {
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
        var optional = new Optional<TestClass>();

        // ReSharper disable once SuspiciousTypeConversion.Global
        var result = optional.Equals((object?)instance);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenObjectEqualsBetweenOptionOfInstanceAndInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
        var optional = new Optional<TestClass>(instance);

        // ReSharper disable once SuspiciousTypeConversion.Global
        var result = optional.Equals((object?)instance);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenObjectEqualsBetweenOptionalOfInstanceAndOptionalOfInstance_ThenReturnsTrue()
    {
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
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
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
        var optional = new Optional<TestClass>();

        var result = optional != instance;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithOtherOptionalAndOtherInstance_ThenReturnsTrue()
    {
        var instance1 = new TestClass
        {
            AStringProperty = "avalue1",
            ADateTimeProperty = DateTime.UtcNow
        };
        var instance2 = new TestClass
        {
            AStringProperty = "avalue2",
            ADateTimeProperty = DateTime.UtcNow
        };
        var optional = new Optional<TestClass>(instance1);

        var result = optional != instance2;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithOptionalOfInstanceAndInstance_ThenReturnsFalse()
    {
        var instance = new TestClass
        {
            AStringProperty = "avalue1",
            ADateTimeProperty = DateTime.UtcNow
        };
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
        var instance = new TestClass { AStringProperty = "avalue", ADateTimeProperty = DateTime.UtcNow };
        var optional = new Optional<TestClass>();

        var result = instance != optional;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithInstanceAndOptionalOfOtherInstance_ThenReturnsTrue()
    {
        var instance1 = new TestClass
        {
            AStringProperty = "avalue1",
            ADateTimeProperty = DateTime.UtcNow
        };
        var instance2 = new TestClass
        {
            AStringProperty = "avalue2",
            ADateTimeProperty = DateTime.UtcNow
        };
        var optional = new Optional<TestClass>(instance1);

        var result = instance2 != optional;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithInstanceAndOptionalOfInstance_ThenReturnsFalse()
    {
        var instance = new TestClass
        {
            AStringProperty = "avalue1",
            ADateTimeProperty = DateTime.UtcNow
        };
        var optional = new Optional<TestClass>(instance);

        var result = instance != optional;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenImplicitCastFromNullableReferenceTypeWithNull_ThenReturnsNone()
    {
        var result = (Optional<string>)(string?)null;

        result.Should().BeNone();
    }

    [Fact]
    public void WhenImplicitCastFromNullableReferenceTypeWithValue_ThenReturnsSome()
    {
        var result = (Optional<string>)"avalue";

        result.Should().BeSome("avalue");
    }

    [Fact]
    public void WhenImplicitCastFromNullableValueTypeWithNull_ThenReturnsNone()
    {
        DateTime? datum = null;

        var result = (Optional<DateTime?>)datum;

        result.Should().BeNone();
    }

    [Fact]
    public void WhenImplicitCastFromNullableValueTypeWithValue_ThenReturnsSome()
    {
        var datum = DateTime.UtcNow;

        var result = (Optional<DateTime>)datum;

        result.Should().BeSome(datum);
    }

    [Fact]
    public void WhenImplicitCastFromOptionalNoneToNullableReferenceType_ThenReturnsNull()
    {
        var result = (string?)Optional<string>.None;

        result.Should().Be(null);
    }

    [Fact]
    public void WhenImplicitCastFromOptionalNullableNoneToNullableReferenceType_ThenReturnsNull()
    {
        var result = (string?)Optional<string?>.None;

        result.Should().Be(null);
    }

    [Fact]
    public void WhenImplicitCastFromOptionalNoneToReferenceType_ThenReturnsDefault()
    {
        var result = (string)Optional<string>.None;

        result.Should().Be(default);
    }

    [Fact]
    public void WhenImplicitCastFromOptionalSomeToNullableReferenceType_ThenReturnsValue()
    {
        var result = (string?)new Optional<string>("avalue");

        result.Should().Be("avalue");
    }

    [Fact]
    public void WhenImplicitCastFromOptionalSomeToReferenceType_ThenReturnsValue()
    {
        var result = (string)new Optional<string>("avalue");

        result.Should().Be("avalue");
    }

    [Fact]
    public void WhenImplicitCastFromOptionalNullableSomeToReferenceType_ThenReturnsValue()
    {
        var result = (string?)new Optional<string?>("avalue");

        result.Should().Be("avalue");
    }

    [Fact]
    public void WhenImplicitCastFromOptionalNullableNoneToNullableValueType_ThenReturnsNull()
    {
        var result = (DateTime?)Optional<DateTime?>.None;

        result.Should().Be(null);
    }

    [Fact]
    public void WhenImplicitCastFromOptionalNoneToValueType_ThenReturnsDefault()
    {
        var result = (DateTime)Optional<DateTime>.None;

        result.Should().Be(default);
    }

    [Fact]
    public void WhenImplicitCastFromOptionalSomeToNullableValueType_ThenReturnsValue()
    {
        var datum = DateTime.UtcNow;

        var result = (DateTime?)new Optional<DateTime>(datum);

        result.Should().Be(datum);
    }

    [Fact]
    public void WhenImplicitCastFromOptionalNullableSomeToNullableValueType_ThenReturnsNull()
    {
        var datum = (DateTime?)DateTime.UtcNow;

        var result = (DateTime?)new Optional<DateTime?>(datum);

        result.Should().Be(datum);
    }

    [Fact]
    public void WhenImplicitCastFromOptionalSomeToValueType_ThenReturnsValue()
    {
        var datum = DateTime.UtcNow;

        var result = (DateTime)new Optional<DateTime>(datum);

        result.Should().Be(datum);
    }

    [Fact]
    public void WhenImplicitCastFromOptionalNullableSomeToValueType_ThenReturnsValue()
    {
        var datum = (DateTime?)DateTime.UtcNow;

        var result = (DateTime)new Optional<DateTime?>(datum)!;

        result.Should().Be(datum);
    }
}

public class TestClass
{
    public required DateTime ADateTimeProperty { get; set; }

    public DateTime? ANullableDateTimeProperty { get; set; }

    public string? ANullableStringProperty { get; set; }

    public required string AStringProperty { get; set; }
}

public struct TestStruct
{
    public required string AStringProperty { get; set; }

    public string? ANullableStringProperty { get; set; }

    public required DateTime ADateTimeProperty { get; set; }

    public DateTime? ANullableDateTimeProperty { get; set; }
}