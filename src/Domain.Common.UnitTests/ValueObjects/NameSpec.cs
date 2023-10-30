using Common;
using Domain.Common.ValueObjects;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Common.UnitTests.ValueObjects;

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
        var name = Name.Create("aname").Value;

        var result = name.Text;

        result.Should().Be("aname");
    }
}