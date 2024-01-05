using FluentAssertions;
using Infrastructure.Shared.ApplicationServices;
using Xunit;

namespace Infrastructure.Shared.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class TokensServiceSpec
{
    private readonly TokensService _service = new();

    [Fact]
    public void WhenCreateTokenForVerification_ThenReturnsRandomValue()
    {
        var result1 = _service.CreateTokenForVerification();
        var result2 = _service.CreateTokenForVerification();
        var result3 = _service.CreateTokenForVerification();

        result1.Should().NotBeEmpty();
        result1.Should().NotBe(result2);
        result2.Should().NotBe(result3);
        result3.Should().NotBe(result1);
    }

    [Fact]
    public void WhenCreateTokenForPasswordReset_ThenReturnsRandomValue()
    {
        var result1 = _service.CreateTokenForPasswordReset();
        var result2 = _service.CreateTokenForPasswordReset();
        var result3 = _service.CreateTokenForPasswordReset();

        result1.Should().NotBeEmpty();
        result1.Should().NotBe(result2);
        result2.Should().NotBe(result3);
        result3.Should().NotBe(result1);
    }

    [Fact]
    public void WhenCreateTokenForJwtRefresh_ThenReturnsRandomValue()
    {
        var result1 = _service.CreateTokenForJwtRefresh();
        var result2 = _service.CreateTokenForJwtRefresh();
        var result3 = _service.CreateTokenForJwtRefresh();

        result1.Should().NotBeEmpty();
        result1.Should().NotBe(result2);
        result2.Should().NotBe(result3);
        result3.Should().NotBe(result1);
    }
}