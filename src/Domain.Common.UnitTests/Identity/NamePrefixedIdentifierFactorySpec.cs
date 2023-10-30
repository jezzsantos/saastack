using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.Identity;

[Trait("Category", "Unit")]
public class NamePrefixedIdentifierFactorySpec
{
    private readonly TestNamePrefixedIdentifierFactory _factory = new();

    [Fact]
    public void WhenCreateWithUnknownEntityType_ThenReturnsGuid()
    {
        var result = _factory.Create(new UnknownEntity());

        result.ToString().Should().MatchRegex(@"xxx_[\d\w]{10,22}");
    }

    [Fact]
    public void WhenCreateWithKnownEntity_ThenReturnsId()
    {
        var result = _factory.Create(new KnownEntity());

        result.ToString().Should().MatchRegex(@"kno_[\d\w]{10,22}");
    }

    [Fact]
    public void WhenIsValidWithTooShortId_ThenReturnsFalse()
    {
        var result = _factory.IsValid(Identifier.Create("tooshort"));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidWithInvalidPrefix_ThenReturnsFalse()
    {
        var result = _factory.IsValid(Identifier.Create("999_123456789012"));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidWithTooShortRandomPart_ThenReturnsFalse()
    {
        var result = _factory.IsValid(Identifier.Create("xxx_123456789"));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidWithTooLongRandomPart_ThenReturnsFalse()
    {
        var result = _factory.IsValid(Identifier.Create("xxx_12345678901234567890123"));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidWithUnknownPrefix_ThenReturnsTrue()
    {
        var result = _factory.IsValid(Identifier.Create("xxx_123456789012"));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsValidWithKnownPrefix_ThenReturnsTrue()
    {
        var result = _factory.IsValid(Identifier.Create("kno_123456789012"));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsValidWithAnonymousUserId_ThenReturnsTrue()
    {
        var result = _factory.IsValid(CallerConstants.AnonymousUserId.ToId());

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsValidWithKnownSupportedPrefix_ThenReturnsTrue()
    {
        _factory.AddSupportedPrefix("another");

        var result = _factory.IsValid(Identifier.Create("another_123456789012"));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenConvertGuidWithKnownGuid_ThenReturnsConverted()
    {
        var id = NamePrefixedIdentifierFactory.ConvertGuid(new Guid("65dd0b02-170b-4ea1-a5a5-00d2808b9aee"), "known")
            .Value;

        id.Should().Be("known_AgvdZQsXoU6lpQDSgIua7g");
    }
}

public class TestNamePrefixedIdentifierFactory : NamePrefixedIdentifierFactory
{
    public TestNamePrefixedIdentifierFactory() : base(new Dictionary<Type, string>
    {
        { typeof(KnownEntity), "kno" }
    })
    {
    }
}

public class KnownEntity : IIdentifiableEntity
{
    // ReSharper disable once UnassignedGetOnlyAutoProperty
    public ISingleValueObject<string> Id { get; } = Identifier.Create("anid");
}

public class UnknownEntity : IIdentifiableEntity
{
    // ReSharper disable once UnassignedGetOnlyAutoProperty
    public ISingleValueObject<string> Id { get; } = Identifier.Create("anid");
}