using FluentAssertions;
using Xunit;

namespace AncillaryDomain.UnitTests;

[Trait("Category", "Unit")]
public class TemplateArgumentsSpec
{
    [Fact]
    public void WhenCreate_ThenAssigned()
    {
        var result = TemplateArguments.Create(new List<string> { "anarg1", "anarg2" });

        result.Value.Items.Count.Should().Be(2);
        result.Value.Items[0].Should().Be("anarg1");
        result.Value.Items[1].Should().Be("anarg2");
    }
}