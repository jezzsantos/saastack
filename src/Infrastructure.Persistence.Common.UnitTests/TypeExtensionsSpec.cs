using Common.Extensions;
using Domain.Interfaces.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.Common.Extensions;
using Xunit;

namespace Infrastructure.Persistence.Common.UnitTests;

[Trait("Category", "Unit")]
public class TypeExtensionsSpec
{
    [Fact]
    public void WhenIsComplexStorageTypeWithASupportedPrimitiveType_ThenReturnsFalse()
    {
        var result = typeof(string).IsComplexStorageType();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsComplexStorageTypeWithASupportedNullablePrimitiveType_ThenReturnsFalse()
    {
        var result = typeof(int?).IsComplexStorageType();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsComplexStorageTypeWithAnUnSupportedPrimitiveType_ThenReturnsTrue()
    {
        var result = typeof(float).IsComplexStorageType();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsComplexStorageTypeWithAnUnSupportedNullablePrimitiveType_ThenReturnsTrue()
    {
        var result = typeof(float?).IsComplexStorageType();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsComplexStorageTypeWithAnyEnumType_ThenReturnsFalse()
    {
        var result = typeof(TestEnum).IsComplexStorageType();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsComplexStorageTypeWithAnyNullableEnumType_ThenReturnsFalse()
    {
        var result = typeof(TestEnum?).IsComplexStorageType();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsComplexStorageTypeWithAByteArray_ThenReturnsFalse()
    {
        var result = typeof(byte[]).IsComplexStorageType();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsComplexStorageTypeWithAValueObject_ThenReturnsFalse()
    {
        var result = typeof(TestValueObject).IsComplexStorageType();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsNullableEnumWithNonEnumType_ThenReturnsFalse()
    {
        var result = typeof(string).IsNullableEnum();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsNullableEnumWithEnumType_ThenReturnsFalse()
    {
        var result = typeof(TestEnum).IsNullableEnum();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsNullableEnumWithNullableEnumType_ThenReturnsTrue()
    {
        var result = typeof(TestEnum?).IsNullableEnum();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenParseNullableWithNonEnum_ThenThrows()
    {
        typeof(string)
            .Invoking(x => x.ParseNullable(nameof(TestEnum.AValue)))
            .Should().Throw<InvalidOperationException>()
            .WithMessage(Resources.TypeExtensions_InvalidType.Format(typeof(string).ToString()));
    }

    [Fact]
    public void WhenParseNullableWithEnum_ThenThrows()
    {
        typeof(TestEnum)
            .Invoking(x => x.ParseNullable(nameof(TestEnum.AValue)))
            .Should().Throw<InvalidOperationException>()
            .WithMessage(Resources.TypeExtensions_InvalidType.Format(typeof(TestEnum).ToString()));
    }

    [Fact]
    public void WhenParseNullableWithNullableEnumButWrongValue_ThenThrows()
    {
        typeof(TestEnum?)
            .Invoking(x => x.ParseNullable("anunknownvalue"))
            .Should().Throw<ArgumentException>()
            .WithMessage("Requested value 'anunknownvalue' was not found.");
    }

    [Fact]
    public void WhenParseNullableWithNullableEnum_ThenReturnsValue()
    {
        var result = typeof(TestEnum?).ParseNullable(nameof(TestEnum.AValue));

        result.Should().Be(TestEnum.AValue);
    }
}

public enum TestEnum
{
    AValue = 0
}

public class TestValueObject : IDehydratableValueObject
{
    public string Dehydrate()
    {
        throw new NotImplementedException();
    }
}