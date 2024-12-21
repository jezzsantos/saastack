using System.Net;
using Common;
using FluentAssertions;
using Infrastructure.Web.Interfaces;
using Xunit;

namespace Infrastructure.Web.Common.UnitTests;

[Trait("Category", "Unit")]
public class StatusCodeSpec
{
    [Fact]
    public void WhenConstructedWithKnownSuccessHttpStatusCode_ThenInitializes()
    {
        var result = new StatusCode(HttpStatusCode.OK);

        result.Code.Should().Be(HttpStatusCode.OK);
        result.Numeric.Should().Be(200);
        result.Reason.Should().Be(Interfaces.Resources.HttpConstants_StatusCodes_Reason_OK);
        result.Title.Should().Be(Interfaces.Resources.HttpConstants_StatusCodes_Title_OK);
        result.ErrorCodes.Should().BeNull();
        result.HttpErrorCode.Should().BeNull();
    }

    [Fact]
    public void WhenConstructedWithUnknownSuccessHttpStatusCode_ThenInitializes()
    {
        var result = new StatusCode(HttpStatusCode.NonAuthoritativeInformation);

        result.Code.Should().Be(HttpStatusCode.NonAuthoritativeInformation);
        result.Numeric.Should().Be(203);
        result.Reason.Should().Be("NonAuthoritativeInformation");
        result.Title.Should().Be("NonAuthoritativeInformation");
        result.ErrorCodes.Should().BeNull();
        result.HttpErrorCode.Should().BeNull();
    }

    [Fact]
    public void WhenConstructedWithKnownErrorHttpStatusCode_ThenInitializes()
    {
        var result = new StatusCode(HttpStatusCode.InternalServerError);

        result.Code.Should().Be(HttpStatusCode.InternalServerError);
        result.Numeric.Should().Be(500);
        result.Reason.Should().Be(Interfaces.Resources.HttpConstants_StatusCodes_Reason_InternalServerError);
        result.Title.Should().Be(Interfaces.Resources.HttpConstants_StatusCodes_Title_InternalServerError);
        result.ErrorCodes.Should().OnlyContain(code => code == ErrorCode.Unexpected);
        result.HttpErrorCode.Should().Be(HttpErrorCode.InternalServerError);
    }
}