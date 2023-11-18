using System.Linq.Expressions;
using FluentAssertions;
using JetBrains.Annotations;
using UnitTesting.Common.Validation;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class ReflectorSpec
{
    [Fact]
    public void WhenGetPropertyNameForSimpleProperty_ThenReturnsName()
    {
        Expression<Func<TestReflectionClass, string>> propertyName = @class => @class.AStringValue;

        var result = Reflector.GetPropertyName(propertyName);

        result.Should().Be(nameof(TestReflectionClass.AStringValue));
    }

    [Fact]
    public void WhenGetPropertyNameForOptionalProperty_ThenReturnsName()
    {
        Expression<Func<TestReflectionClass, string>> propertyName = @class => @class.AnOptionalStringValue;

        var result = Reflector.GetPropertyName(propertyName);

        result.Should().Be(nameof(TestReflectionClass.AnOptionalStringValue));
    }

    [Fact]
    public void WhenGetPropertyNameForAMethod_ThenThrows()
    {
        Expression<Func<TestReflectionClass, string>> propertyName = @class => @class.AMethod();

        FluentActions.Invoking(() => Reflector.GetPropertyName(propertyName))
            .Should().Throw<ArgumentException>()
            .WithMessageLike(Resources.Reflector_ErrorNotMemberAccessOrConvertible);
    }
}

[UsedImplicitly]
internal class TestReflectionClass
{
    public Optional<string> AnOptionalStringValue { get; } = null!;

    public string AStringValue { get; } = null!;

    public string AMethod()
    {
        return string.Empty;
    }
}