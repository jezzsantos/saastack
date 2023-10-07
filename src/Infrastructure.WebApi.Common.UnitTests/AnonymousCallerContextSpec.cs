using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Infrastructure.WebApi.Common.UnitTests;

[Trait("Category", "Unit")]
public class AnonymousCallerContextSpec
{
    [Fact]
    public void WhenConstructedAndHttpRequestHasNoCorrelationId_ThenFabricatesCallId()
    {
        var httpContext = new Mock<IHttpContextAccessor>();
        httpContext.Setup(hc => hc.HttpContext!.Items).Returns(new Dictionary<object, object?>());

        var result = new AnonymousCallerContext(httpContext.Object);

        result.CallId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WhenConstructedAndHttpRequestHasCorrelationId_ThenSetsCallId()
    {
        var httpContext = new Mock<IHttpContextAccessor>();
        httpContext.Setup(hc => hc.HttpContext!.Items).Returns(new Dictionary<object, object?>
        {
            { RequestCorrelationFilter.CorrelationIdItemName, "acorrelationid" }
        });

        var result = new AnonymousCallerContext(httpContext.Object);

        result.CallId.Should().Be("acorrelationid");
    }
}