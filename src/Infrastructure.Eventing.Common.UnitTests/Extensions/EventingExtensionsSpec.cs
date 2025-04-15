using FluentAssertions;
using Infrastructure.Eventing.Common.Extensions;
using Xunit;

namespace Infrastructure.Eventing.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class EventingExtensionsSpec
{
    [Fact]
    public void WhenCreateConsumerNameWithBasicNames_ThenReturnsName()
    {
        var result = EventingExtensions.CreateConsumerName("afulltypename", "anassemblyname");

        result.Should().Be("anassemblyname-afulltypename");
    }

    [Fact]
    public void WhenCreateConsumerNameWithMultiPartNames_ThenReturnsName()
    {
        var result = EventingExtensions.CreateConsumerName("a.full.type.name", "an.assembly.name");

        result.Should().Be("an-assembly-name-a-full-type-name");
    }

    [Fact]
    public void WhenCreateConsumerNameWithTooLongNames_ThenReturnsTruncatedName()
    {
        var result =
            EventingExtensions.CreateConsumerName("a.very.very.very.long.too.long.full.type.name", "an.assembly.name");

        result.Should().Be("an-assembly-name-a-very-very-very-long-too-long-fu");
    }

    [Fact]
    public void WhenCreateConsumerNameWithKnownFillerWords_ThenReturnsTruncatedName()
    {
        var result =
            EventingExtensions.CreateConsumerName("asubdomainInfrastructure.NotificationConsumer", "an.assembly.name");

        result.Should().Be("an-assembly-name-asubdomain");
    }
}