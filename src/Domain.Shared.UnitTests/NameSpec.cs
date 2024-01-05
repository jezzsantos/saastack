using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests;

[Trait("Category", "Unit")]
public class NameSpec
{
    [Fact]
    public void WhenConstructWithEmptyName_ThenReturnsError()
    {
        var result = Name.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenText_ThenReturnsValue()
    {
        var result = Name.Create("aname").Value;

        result.Text.Should().Be("aname");
    }
}