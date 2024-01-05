using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests;

[Trait("Category", "Unit")]
public class PersonDisplayNameSpec
{
    [Fact]
    public void WhenConstructWithEmptyName_ThenReturnsError()
    {
        var result = PersonDisplayName.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenText_ThenReturnsValue()
    {
        var result = PersonDisplayName.Create("aname").Value;

        result.Text.Should().Be("aname");
    }
}