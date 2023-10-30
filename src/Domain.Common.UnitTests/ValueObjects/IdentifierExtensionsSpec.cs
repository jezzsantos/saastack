using Domain.Common.Identity;
using Domain.Common.UnitTests.Identity;
using Domain.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.ValueObjects;

[Trait("Category", "Unit")]
public class IdentifierExtensionsSpec
{
    [Fact]
    public void WhenToIdentifierWithEmptyString_ThenReturnsEmpty()
    {
        var result = string.Empty.ToId();

        result.Should().Be(Identifier.Create(string.Empty));
        result.Should().Be(Identifier.Empty());
        result.IsEmpty().Should().BeTrue();
    }

    [Fact]
    public void WhenToIdentifierWithAnyString_ThenReturns()
    {
        var result = "avalue".ToId();

        result.Should().Be(Identifier.Create("avalue"));
        result.IsEmpty().Should().BeFalse();
    }

    [Fact]
    public void WhenToIdentifierFactoryWithStringValue_ThenReturnsFixedFactory()
    {
        var result = "anid".ToIdentifierFactory();

        result.Should().BeOfType<FixedIdentifierFactory>();
        result.Create(new KnownEntity()).Value.Should().Be("anid".ToId());
    }

    [Fact]
    public void WhenToIdentifierFactoryWithIdentifier_ThenReturnsFixedFactory()
    {
        var result = "anid".ToId().ToIdentifierFactory();

        result.Should().BeOfType<FixedIdentifierFactory>();
        result.Create(new KnownEntity()).Value.Should().Be("anid".ToId());
    }
}