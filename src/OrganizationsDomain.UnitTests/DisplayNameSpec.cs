using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class DisplayNameSpec
{
    [Fact]
    public void WhenCreateWithInvalidName_ThenReturnsError()
    {
        var result = DisplayName.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation, Resources.OrganizationDisplayName_InvalidName);
    }

    [Fact]
    public void WhenCreate_ThenReturnsName()
    {
        var result = DisplayName.Create("aname");

        result.Should().BeSuccess();
        result.Value.Name.Should().Be("aname");
    }
}