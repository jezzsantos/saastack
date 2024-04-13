using Common;
using Domain.Common.ValueObjects;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace UserProfilesDomain.UnitTests;

[Trait("Category", "Unit")]
public class AvatarSpec
{
    [Fact]
    public void WhenCreateAndNoUrl_ThenReturnsError()
    {
        var result = Avatar.Create("animageid".ToId(), string.Empty);

        result.Should().BeError(ErrorCode.Validation, Resources.Avatar_InvalidUrl);
    }

    [Fact]
    public void WhenCreate_ThenReturnsAvatar()
    {
        var result = Avatar.Create("animageid".ToId(), "aurl");

        result.Should().BeSuccess();
        result.Value.ImageId.Should().Be("animageid".ToId());
        result.Value.Url.Should().Be("aurl");
    }
}